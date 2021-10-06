using Magento;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(magestack.Startup))]

namespace magestack
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            Magestack server = new Magestack();

            builder.Services.AddSingleton(server.Database);
            builder.Services.AddSingleton(server.Sftp);
            builder.Services.AddSingleton(server.Ssh);
        }
    }
}