﻿using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Pgd.Magento;
using Renci.SshNet;
using StackExchange.Redis;
using System;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Pgd.Magento
{
    /// <summary>
    /// Class representing startup behavior for Azure function environment
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Configuration method for the Azure function environment
        /// </summary>
        /// <param name="builder">Function builder for dependency injection</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            DefaultAzureCredential creds = new();
            Uri keyvaultUri = new(Environment.GetEnvironmentVariable("vault-uri"));
            SecretClient secretClient = new(keyvaultUri, creds);

            ConnectionInfo sshConnectionInfo = GetSshCredentials(secretClient);

            SshClient ssh = new(sshConnectionInfo)
            {
                KeepAliveInterval = new(0, 1, 0)
            };

            string cs = ConnectDb(secretClient, ssh);

            KeyVaultSecret cacheUri = secretClient.GetSecret("cache-uri");
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(cacheUri.Value);

            builder.Services.AddSingleton(cs);
            builder.Services.AddSingleton(ssh);
            builder.Services.AddSingleton(sshConnectionInfo);
            builder.Services.AddSingleton(redis);

            builder.Services.AddAzureClients(clientBuilder =>
            {
                clientBuilder.UseCredential(new DefaultAzureCredential());
                clientBuilder.AddSecretClient(keyvaultUri);

                clientBuilder.AddQueueServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            });

            builder.Services.AddHttpClient("wsi", config =>
            {
                KeyVaultSecret wsiUri = secretClient.GetSecret("wsi-uri");
                KeyVaultSecret wsiKey = secretClient.GetSecret("wsi-key");

                config.BaseAddress = new(wsiUri.Value);
                config.DefaultRequestHeaders.Add("x-functions-key", wsiKey.Value);
            });
        }

        private static string ConnectDb(SecretClient secretClient, SshClient ssh)
        {
            KeyVaultSecret dbHost = secretClient.GetSecret("db-host");
            KeyVaultSecret dbUser = secretClient.GetSecret("db-user");
            KeyVaultSecret dbPass = secretClient.GetSecret("db-pass");
            KeyVaultSecret dbSchema = secretClient.GetSecret("db-schema");

            ssh.Connect();

            ForwardedPortLocal forwardedPort = new("127.0.0.1",
                dbHost.Value,
                3306);
            ssh.AddForwardedPort(forwardedPort);
            forwardedPort.Start();

            MySqlConnectionStringBuilder cnxString = new()
            {
                Server = forwardedPort.BoundHost,
                Port = forwardedPort.BoundPort,
                UserID = dbUser.Value,
                Password = dbPass.Value,
                Database = dbSchema.Value,
                Pooling = true,
                MinimumPoolSize = 3
            };

            return cnxString.ToString();
        }

        /// <summary>
        /// Retrieves the credentials for authenticating to a server using SSH from a KeyVault
        /// </summary>
        /// <param name="secretClient">KeyVault client with connection to KeyVault containing connection information</param>
        /// <returns>ConnectionInfo configured with a PasswordAuthenticationMethod</returns>
        private static ConnectionInfo GetSshCredentials(SecretClient secretClient)
        {
            KeyVaultSecret stackHost = secretClient.GetSecret("stack-host");
            KeyVaultSecret stackUser = secretClient.GetSecret("stack-user");
            KeyVaultSecret stackPass = secretClient.GetSecret("stack-pass");
            KeyVaultSecret stackPort = secretClient.GetSecret("stack-port");

            int port = int.Parse(stackPort.Value);

            PasswordAuthenticationMethod passwordAuth = new(stackUser.Value, stackPass.Value);

            return new(stackHost.Value, port, stackUser.Value, passwordAuth);
        }
    }
}