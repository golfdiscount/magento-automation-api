using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace magestack
{
    /// <summary>
    /// An instance of a timer trigger to upload files to WSI
    /// </summary>
    public class WsiTimer
    {
        [FunctionName("triggerUploadOrders")]
        [Singleton]
        public async Task Run(
            [TimerTrigger("45 15, 03 * * *")]TimerInfo myTimer,
            ILogger log)
        {
            // Timeout is set to 5 minutes as uploading orders can take a while
            HttpClient requester = new HttpClient { 
                Timeout = new TimeSpan(0, 5, 0) 
            };

            log.LogInformation("Pinged API to start upload of WSI orders");

            await requester.GetAsync(
                Environment.GetEnvironmentVariable("magestack_func_url") + "/uploadOrders"
                );
        }
    }
}