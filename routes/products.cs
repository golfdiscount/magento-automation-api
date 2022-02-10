using Magento;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace magestack.routes
{
    public class Products
    {
        private readonly MagentoDb _db_cnx;
        public Products(MagentoDb dbCnx)
        {
            _db_cnx = dbCnx;
        }

        [FunctionName("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{sku}")] HttpRequest req,
            string sku,
            ILogger log)
        {
            log.LogInformation($"Searching for {sku} in the database");

            string qry = $@"SELECT v1.value AS 'name', e.sku,
            FORMAT(d1.value, 2) AS 'price', t1.value AS 'short_description'
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
            WHERE e.sku = '{sku}';";

            MySqlDataReader db_res = _db_cnx.ExecuteDbCommand(qry);
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (!db_res.HasRows)
            {
                db_res.Close();
                return new NotFoundObjectResult($"{sku} was not found in Magento");
            }

            while(db_res.Read())
            {

                result.Add("name", db_res.GetString(0));
                result.Add("sku", db_res.GetString(1));
                result.Add("price", db_res.GetString(2));
                result.Add("description", db_res.GetString(3));

            }

            db_res.Close();
            return new OkObjectResult(result);
        }
    }
}
