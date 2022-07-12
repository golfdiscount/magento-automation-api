using magestack.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace magestack.Queue_Triggers
{
    public class CacheProduct
    {
        private readonly IDistributedCache cache;

        public CacheProduct(IDistributedCache cache)
        {
            this.cache = cache;
        }

        [FunctionName("CacheProduct")]
        public void Run([QueueTrigger("cache-products")]string queueItem, ILogger log)
        {
            Product product = JsonSerializer.Deserialize<Product>(queueItem);

            DistributedCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = new TimeSpan(24, 0, 0)
            };

            log.LogInformation($"Caching {product.sku} in Redis cache");
            cache.SetString(product.sku, queueItem, options);
        }
    }
}
