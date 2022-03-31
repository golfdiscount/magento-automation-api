using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace magestack.routes
{
    public class Products
    {
        private readonly String _cs;
        public Products(String cs)
        {
            _cs = cs;
        }

        [FunctionName("GetProducts")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{sku}")] HttpRequest req,
            string sku,
            ILogger log)
        {
            IActionResult res;
            log.LogInformation($"Searching for {sku} in the database");

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

                Dictionary<string, string> result = new Dictionary<string, string>();

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        res = new NotFoundObjectResult($"{sku} was not found in Magento");
                    } else
                    {
                        while (reader.Read())
                        {
                            result.Add("name", reader.GetString(0));
                            result.Add("sku", reader.GetString(1));
                            result.Add("price", reader.GetString(2));
                            result.Add("description", reader.GetString(3));
                            result.Add("upc", reader.GetString(4));
                            result.Add("quantity", reader.GetString(5));
                        }
                    }
                }
                res = new OkObjectResult(result);
            }
            
            return res;
        }
    }
}
