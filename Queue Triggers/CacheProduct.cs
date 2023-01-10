using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Pgd.Magento.Models;
using StackExchange.Redis;
using System;
using System.Text.Json;

namespace Pgd.Magento.QueueTriggers
{
    public class CacheProduct
    {
        private readonly IDatabase _redisDb;

        public CacheProduct(ConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        [FunctionName("CacheProduct")]
        public void Run([QueueTrigger("cache-products")]string queueItem, ILogger log)
        {
            ProductModel product = JsonSerializer.Deserialize<ProductModel>(queueItem);

            DistributedCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = new TimeSpan(5, 0, 0, 0)
            };

            log.LogInformation($"Caching {product.Sku} in Redis cache");
            _redisDb.StringSet(product.Sku, queueItem, new TimeSpan(5, 0, 0, 0));
        }
    }
}
