using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using System;
using System.Diagnostics;

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
            Uri keyvaultUri = new("https://magestack.vault.azure.net/");
            SecretClient secretClient = new(keyvaultUri, creds);

            string cs = ConnectDb(secretClient);
            SftpClient sftp = ConnectSftp(secretClient);

            builder.Services.AddSingleton(cs);
            builder.Services.AddSingleton(sftp);
            builder.Services.AddHttpClient();

            builder.Services.AddDistributedRedisCache(config =>
            {
                config.Configuration = Environment.GetEnvironmentVariable("cache");
            });
        }

        private static string ConnectDb(SecretClient secretClient)
        {
            KeyVaultSecret dbHost = secretClient.GetSecret("db-host");
            KeyVaultSecret dbUser = secretClient.GetSecret("db-user");
            KeyVaultSecret dbPass = secretClient.GetSecret("db-pass");
            KeyVaultSecret dbPort = secretClient.GetSecret("db-port");

            SshClient ssh = ConnectSsh(secretClient);
            ssh.Connect();

            // Bind dbHost:dbPort to 127.0.0.1:bound_port
            // Helps tunnel for MySQL connection
            ForwardedPortLocal forwardedPort = new("127.0.0.1",
                uint.Parse(Environment.GetEnvironmentVariable("bound_port")),
                dbHost.Value,
                uint.Parse(dbPort.Value));
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
            if (Debugger.IsAttached)
            {
                string stackHost = Environment.GetEnvironmentVariable("stack-host");
                string stackPort = Environment.GetEnvironmentVariable("stack-port");
                string stackUser = Environment.GetEnvironmentVariable("stack-user");
                string stackPass = Environment.GetEnvironmentVariable("stack-pass");

                return new SftpClient(stackHost, int.Parse(stackPort), stackUser, stackPass);
            } else
            {
                KeyVaultSecret stackHost = secretClient.GetSecret("stack-host");
                KeyVaultSecret stackPort = secretClient.GetSecret("stack-port");
                KeyVaultSecret stackUser = secretClient.GetSecret("stack-user");
                KeyVaultSecret stackPass = secretClient.GetSecret("stack-pass");

                return new SftpClient(stackHost.Value,
                    int.Parse(stackPort.Value),
                    stackUser.Value,
                    stackPass.Value);
            }
        }

        private static SshClient ConnectSsh(SecretClient secretClient)
        {
            if (Debugger.IsAttached)
            {
                string stackHost = Environment.GetEnvironmentVariable("stack-host");
                string stackPort = Environment.GetEnvironmentVariable("stack-port");
                string stackUser = Environment.GetEnvironmentVariable("stack-user");
                string stackPass = Environment.GetEnvironmentVariable("stack-pass");

                return new SshClient(stackHost, int.Parse(stackPort), stackUser, stackPass)
                {
                    KeepAliveInterval = new TimeSpan(0, 1, 0)
                };
            } else
            {
                KeyVaultSecret stackHost = secretClient.GetSecret("stack-host");
                KeyVaultSecret stackPort = secretClient.GetSecret("stack-port");
                KeyVaultSecret stackUser = secretClient.GetSecret("stack-user");
                KeyVaultSecret stackPass = secretClient.GetSecret("stack-pass");

                return new SshClient(stackHost.Value, int.Parse(stackPort.Value), stackUser.Value, stackPass.Value)
                {
                    KeepAliveInterval = new TimeSpan(0, 1, 0)
                };
            }
        }
    }
}