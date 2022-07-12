using Azure.Storage.Queues;
using magestack.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace magestack.Timer_Triggers
{
    public class CacheMagentoProducts
    {
        private readonly string cs;
        private readonly IDistributedCache cache;
        public CacheMagentoProducts(string cs, IDistributedCache cache)
        {
            this.cs = cs;
            this.cache = cache;
        }

        [FunctionName("CacheMagentoProducts")]
        public void Run([TimerTrigger("0 0,30 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Searching for Magento products in orders for the last 30 minutes");

            string query = @"SELECT v1.value AS 'name',
				   e.sku,
				   FORMAT(d1.value, 2) AS 'price',
				   t1.value AS 'short_description',
				   v2.value AS 'upc'
			FROM catalog_product_entity e
			LEFT JOIN catalog_product_entity_varchar v1 ON e.entity_id = v1.entity_id
			AND v1.store_id = 0
			AND v1.attribute_id = (
				SELECT attribute_id
				FROM eav_attribute
				WHERE attribute_code = 'name'
					AND entity_type_id = (
						SELECT entity_type_id
						FROM eav_entity_type
						WHERE entity_type_code = 'catalog_product'))
			LEFT JOIN catalog_product_entity_text t1 ON e.entity_id = t1.entity_id
			AND t1.store_id = 0
			AND t1.attribute_id = (
				SELECT attribute_id
				FROM eav_attribute
				WHERE attribute_code = 'short_description'
					AND entity_type_id = (
						SELECT entity_type_id
						FROM eav_entity_type
						WHERE entity_type_code = 'catalog_product'))
			LEFT JOIN catalog_product_entity_decimal d1 ON e.entity_id = d1.entity_id
			AND d1.store_id = 0
			AND d1.attribute_id = (
				SELECT attribute_id
				FROM eav_attribute
				WHERE attribute_code = 'price'
					AND entity_type_id = (
						SELECT entity_type_id
						FROM eav_entity_type
						WHERE entity_type_code = 'catalog_product'))
			LEFT JOIN catalog_product_entity_varchar v2 ON e.entity_id = v2.entity_id
			AND v2.store_id = 0
			AND v2.attribute_id = (
				SELECT attribute_id
				FROM eav_attribute
				WHERE attribute_code = 'upc'
					AND entity_type_id = (
						SELECT entity_type_id
						FROM eav_entity_type
						WHERE entity_type_code = 'catalog_product'))
			JOIN(SELECT *
			FROM sales_order_item
			WHERE product_type = 'simple'
				AND created_at >= SUBDATE(NOW(), INTERVAL 30 MINUTE)
			LIMIT 100) AS recent_products ON recent_products.sku = e.sku;";
			MySqlDataReader reader = MySqlHelper.ExecuteReader(cs, query);

			List<Product> productsToCache = new();

			while (reader.Read())
            {
				Product product = new()
                {
					name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
					sku = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
					price = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
					description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
					upc = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
				};

				string productInfo = cache.GetString(product.sku);
				
				if (productInfo == null)
                {
					productsToCache.Add(product);
                }
			}

			log.LogInformation($"{productsToCache.Count} product(s) not in cache");

			foreach(Product product in productsToCache)
            {
				log.LogInformation($"Caching {product.sku}");
				QueueProduct(product);
            };
		}

		private static void QueueProduct(Product product)
		{
			string productInfo = JsonSerializer.Serialize(product);
			productInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(productInfo));
			QueueClient client = new(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "cache-products");
			client.SendMessage(productInfo);
		}
	}
}
