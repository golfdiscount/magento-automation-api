using Microsoft.Azure.WebJobs;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace magestack
{
    public class WsiTimer
    {
        [FunctionName("triggerUploadOrders")]
        [Singleton]
        public async Task Run(
            [TimerTrigger("45 15 * * *")]TimerInfo myTimer)
        {
            HttpClient requester = new HttpClient { 
                Timeout = new TimeSpan(0, 5, 0) 
            };

            await requester.GetAsync(
                Environment.GetEnvironmentVariable("magestack_func_url") + "uploadOrders"
                );
        }
    }
}