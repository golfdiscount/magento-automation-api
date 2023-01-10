using magestack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Text;
using System.Text.Json;
using magestack.Data;

namespace magestack.routes
{
    public class GetMagentoProduct
    {
        private readonly string cs;
        private readonly IDistributedCache cache;
        public GetMagentoProduct(string cs, IDistributedCache cache)
        {
            this.cs = cs;
            this.cache = cache;
        }

        [FunctionName("GetMagentoProducts")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{sku}")] HttpRequest req,
            string sku,
            ILogger log)
        {
            ProductModel product = new();
            log.LogInformation($"Searching for {sku} in Redis cache");

            try
            {
                string productInfo = cache.GetString(sku);

                if (productInfo != null)
                {
                    log.LogInformation($"Found {sku} in Redis cache");
                    return new JsonResult(JsonSerializer.Deserialize<ProductModel>(productInfo));
                }
            }
            catch
            {
                log.LogError($"Unable to query cache");
            }

            log.LogWarning($"Unable to find {sku} in cache, searching in database");

            using MySqlConnection conn = new(cs);
            conn.Open();
            product = Products.GetProduct(sku, conn);

            if (product == null)
            {
                return new NotFoundObjectResult($"{sku} was not found in Magento");
            }

            return new OkObjectResult(product);
            
        }

/*        private static void QueueProduct(Product product)
        {
            string productInfo = JsonSerializer.Serialize(product);
            productInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(productInfo));
            QueueClient client = new(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "cache-products");
            client.SendMessage(productInfo);
        }*/
    }
}
