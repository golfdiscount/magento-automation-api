using Magento;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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
            Magestack server = new Magestack();

            builder.Services.AddSingleton(server.Db);
            builder.Services.AddSingleton(server.Sftp);
            builder.Services.AddSingleton(server.Ssh);
        }
    }
}