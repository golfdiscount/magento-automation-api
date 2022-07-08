using Azure.Storage.Queues;
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

namespace magestack.routes
{
    public class GetMagentoProducts
    {
        private readonly string _cs;
        private readonly IDistributedCache cache;
        public GetMagentoProducts(string cs, IDistributedCache cache)
        {
            _cs = cs;
            this.cache = cache;
        }

        [FunctionName("GetMagentoProducts")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{sku}")] HttpRequest req,
            string sku,
            ILogger log)
        {
            Product product = new Product();
            log.LogInformation($"Searching for {sku} in Redis cache");

            string productInfo = cache.GetString(sku);

            if (productInfo != null)
            {
                log.LogInformation($"Found {sku} in Redis cache");
                return new JsonResult(JsonSerializer.Deserialize<Product>(productInfo));
            }

            log.LogWarning($"Unable to find {sku} in cache, searching in database");

            using (MySqlConnection cxn = new MySqlConnection(_cs))
            using (MySqlCommand cmd = cxn.CreateCommand())
            {
                cxn.Open();
                cmd.CommandText = $@"SELECT v1.value AS 'name',
                    e.sku,
                    FORMAT(d1.value, 2) AS 'price',
                    t1.value AS 'short_description',
                    v2.value AS 'upc',
                    FORMAT(inv.qty, 0) AS 'qty'
                FROM catalog_product_entity e
                LEFT JOIN catalog_product_entity_varchar v1 ON e.entity_id = v1.entity_id
                AND v1.store_id = 0
                AND v1.attribute_id =(
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
                AND v2.attribute_id =(
	                SELECT attribute_id
	                FROM eav_attribute
	                WHERE attribute_code = 'upc'
		                AND entity_type_id = (
			                SELECT entity_type_id
			                FROM eav_entity_type
			                WHERE entity_type_code = 'catalog_product'))
                LEFT JOIN cataloginventory_stock_item AS inv ON e.entity_id = inv.product_id
                WHERE e.sku = '{sku}';";

                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    product.name = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                    product.sku = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    product.price = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                    product.description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                    product.upc = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                    product.quantity = reader.IsDBNull(5) ? 0 : reader.GetInt16(5);

                    log.LogInformation($"Queueing {sku} to be cached");
                    QueueProduct(product);

                    return new OkObjectResult(product);
                }
            }

            log.LogWarning($"Unable to find {sku} in database");
            return new NotFoundObjectResult($"{sku} was not found in Magento"); ;
        }

        private void QueueProduct(Product product)
        {
            string productInfo = JsonSerializer.Serialize(product);
            productInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(productInfo));
            QueueClient client = new QueueClient("DefaultEndpointsProtocol=https;AccountName=magestacktest;AccountKey=VHCa9lG/uNc5h36bQNDmCVrngQ2WP7PP7yuaQhIaSIgl3TNEefpxFVNA8K/9mMt2ZwV8cT/pDVBbhk7SPn6/7g==;BlobEndpoint=https://magestacktest.blob.core.windows.net/;QueueEndpoint=https://magestacktest.queue.core.windows.net/;TableEndpoint=https://magestacktest.table.core.windows.net/;FileEndpoint=https://magestacktest.file.core.windows.net/;", "cache-products");
            client.SendMessage(productInfo);
        }
    }
}
