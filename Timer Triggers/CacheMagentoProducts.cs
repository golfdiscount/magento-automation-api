using Azure.Storage.Queues;
using magestack.Data;
using magestack.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using StackExchange.Redis;
using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace magestack.Timer_Triggers
{
    public class CacheMagentoProducts
    {
        private readonly string cs;
		private readonly IDatabase _redisDb;
		private readonly QueueClient productCacheQueue;
        public CacheMagentoProducts(string cs, ConnectionMultiplexer redis, QueueServiceClient queueClient)
        {
            this.cs = cs;
            _redisDb = redis.GetDatabase();
			productCacheQueue = queueClient.GetQueueClient("cache-products");
        }

        [FunctionName("CacheMagentoProducts")]
        public void Run([TimerTrigger("0 0,30 * * * *")]TimerInfo myTimer, ILogger log)
        {
			log.LogInformation($"Searching for recent magento products");

			using MySqlConnection conn = new(cs);
			conn.Open();

			using MySqlCommand cmd = conn.CreateCommand();
			cmd.CommandText = "SELECT DISTINCT sku " +
				"FROM(SELECT sku, created_at " +
					"FROM sales_order_item " +
					"WHERE product_type = 'simple' " +
					"ORDER BY created_at DESC " +
					"LIMIT 100) AS recent_products";

            using MySqlDataReader reader = cmd.ExecuteReader();

			List<string> skus = new();
			List<ProductModel> productsToCache = new();

			while (reader.Read())
			{
				string sku = reader.GetString("sku");
                string productInfo = _redisDb.StringGet(sku);

				if (productInfo == null)
				{
					skus.Add(sku);
				}
			}

			reader.Close();

			log.LogInformation($"{skus.Count} product(s) not in cache");

			foreach (string sku in skus)
			{
                log.LogInformation($"Caching {sku}");
				QueueProduct(Products.GetProduct(sku, conn));
            }
		}

		private void QueueProduct(ProductModel product)
		{
			string productInfo = JsonSerializer.Serialize(product);
			productInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(productInfo));
			productCacheQueue.SendMessage(productInfo);
		}
	}
}
