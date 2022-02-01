using Magento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace magestack
{
    /// <summary> Trigger for getting product information </summary>
    public class ProductInfo
    {
        private readonly MagentoDb _db;

        /// <summary>
        /// Initializes the trigger
        /// </summary>
        /// <param name="db"> Connection to Magento database</param>
        public ProductInfo(MagentoDb db)
        {
            _db = db;
        }

        /// <summary>
        /// Triggers a run of the getProductInfo trigger
        /// </summary>
        /// <param name="req"> Http Request object </param>
        /// <param name="sku"> SKU to get information for </param>
        /// <returns></returns>
        [FunctionName("getProductInfo")]
        public JsonResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{sku}")] HttpRequest req,
            string sku)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            string qry = "SELECT `value` AS \"name\"\n" +
                "FROM catalog_product_entity AS product " +
                "JOIN catalog_product_entity_varchar AS product_var ON product_var.entity_id = product.entity_id\n" +
                $"WHERE sku = {sku}\n" +
                "AND product_var.attribute_id = (\n" +
                "# Get attribute ID for \"Product Name\" field\n" +
                    "SELECT attribute_id \n" +
                    "FROM eav_attribute \n" +
                    "WHERE frontend_label = \"Product Name\");";

            using (MySqlDataReader reader = _db.ExecuteDbCommand(qry))
            {
                while (reader.Read())
                {
                    results.Add("name", reader.GetString("name"));
                }
            }

            return new JsonResult(results);
        }
    }
}