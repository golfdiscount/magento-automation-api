using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace magestack.HTTP_Triggers
{
    public static class TestIp
    {
        [FunctionName("TestIp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var client = new HttpClient();
            var response = await client.GetAsync("https://ifocnfig.me");
            var responseMessage = await response.Content.ReadAsStringAsync();

            return new OkObjectResult(responseMessage);
        }
    }
}
