using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using System;

[assembly: FunctionsStartup(typeof(magestack.Startup))]

namespace magestack
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

            string cs = ConnectDb(secretClient);
            SftpClient sftp = ConnectSftp(secretClient);

            builder.Services.AddSingleton(cs);
            builder.Services.AddSingleton(sftp);
            builder.Services.AddHttpClient();

            builder.Services.AddDistributedRedisCache(config =>
            {
                KeyVaultSecret cacheUri = secretClient.GetSecret("cache-uri");
                config.Configuration = cacheUri.Value;
            });

            builder.Services.AddAzureClients(clientBuilder =>
            {
                Uri vaultUri = new(Environment.GetEnvironmentVariable("vault-uri"));
                clientBuilder.AddSecretClient(vaultUri);
            });
        }

        private static string ConnectDb(SecretClient secretClient)
        {
            KeyVaultSecret dbHost = secretClient.GetSecret("db-host");
            KeyVaultSecret dbUser = secretClient.GetSecret("db-user");
            KeyVaultSecret dbPass = secretClient.GetSecret("db-pass");

            SshClient ssh = ConnectSsh(secretClient);
            ssh.Connect();

            ForwardedPortLocal forwardedPort = new("127.0.0.1",
                uint.Parse(Environment.GetEnvironmentVariable("bound_port")),
                dbHost.Value,
                3306);
            ssh.AddForwardedPort(forwardedPort);
            forwardedPort.Start();

            MySqlConnectionStringBuilder cnxString = new()
            {
                Server = "127.0.0.1",
                Port = uint.Parse(Environment.GetEnvironmentVariable("bound_port")),
                UserID = dbUser.Value,
                Password = dbPass.Value,
                Database = "golfdi_mage2",
                Pooling = true,
                MinimumPoolSize = 3
            };

            return cnxString.ToString();
        }

        private static SftpClient ConnectSftp(SecretClient secretClient)
        {
            KeyVaultSecret stackHost = secretClient.GetSecret("stack-host");
            KeyVaultSecret stackUser = secretClient.GetSecret("stack-user");
            KeyVaultSecret stackPass = secretClient.GetSecret("stack-pass");

            return new SftpClient(stackHost.Value,
                22,
                stackUser.Value,
                stackPass.Value);
        }

        private static SshClient ConnectSsh(SecretClient secretClient)
        {
            KeyVaultSecret stackHost = secretClient.GetSecret("stack-host");
            KeyVaultSecret stackUser = secretClient.GetSecret("stack-user");
            KeyVaultSecret stackPass = secretClient.GetSecret("stack-pass");

            return new SshClient(stackHost.Value, 22, stackUser.Value, stackPass.Value)
            {
                KeepAliveInterval = new TimeSpan(0, 1, 0)
            };
        }
    }
}