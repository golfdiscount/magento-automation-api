using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Pgd.Magento.Models;
using StackExchange.Redis;
using System;
using System.Text.Json;

namespace Pgd.Magento.QueueTriggers
{
    public class CacheProduct
    {
        private readonly ConnectionMultiplexer _redis;

        public CacheProduct(ConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        [FunctionName("CacheProduct")]
        public void Run([QueueTrigger("cache-products")]string queueItem, ILogger log)
        {
            IDatabase db = _redis.GetDatabase();
            ProductModel product = JsonSerializer.Deserialize<ProductModel>(queueItem);

            log.LogInformation($"Caching {product.Sku} in Redis cache");
            db.StringSet(product.Sku, queueItem, new TimeSpan(0, 36, 0, 0));
        }
    }
}
