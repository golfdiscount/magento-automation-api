using Magento;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(magestack.Startup))]

namespace magestack
{
    public class Startup : FunctionsStartup
    {
        public async override void Configure(IFunctionsHostBuilder builder)
        {
            Magestack server = new Magestack();
            SshTunnel ssh = server.CreateSshClient();
            SftpClient sftp = server.CreateSftpClient();

            ssh.ForwardPort(
                "127.0.0.1",
                3307,
                Environment.GetEnvironmentVariable("db_host"),
                uint.Parse(Environment.GetEnvironmentVariable("db_port"))
                );
            await server.CreateMySqlConn("127.0.0.1",
                3307,
                Environment.GetEnvironmentVariable("db_user"),
                Environment.GetEnvironmentVariable("db_pass")
                );

            builder.Services.AddSingleton(server);
            builder.Services.AddSingleton(ssh);
            builder.Services.AddSingleton(sftp);
        }
    }
}