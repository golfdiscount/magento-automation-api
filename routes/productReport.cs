using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.IO;

namespace magestack.routes
{
    public class productReport
    {
        private readonly String _cs;
        private readonly string path = @"export\productReport.csv";

        public productReport(String cs)
        {
            _cs = cs;
        }

        [FunctionName("GenerateExceptionReport")]
        public ActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "exceptions")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Generating exception report from Magento...");
            ActionResult res;

            using (MySqlConnection cxn = new MySqlConnection(_cs))
            using (MySqlCommand cmd = cxn.CreateCommand())
            {
                string results = "";
                cxn.Open();
                cmd.CommandText = $@"SELECT product.sku,
	                FORMAT(inventory.qty, 0) AS 'quantity',
                    reservation.reserved
                    FROM catalog_product_entity AS product
                JOIN cataloginventory_stock_status AS inventory ON inventory.product_id = product.entity_id
                RIGHT JOIN(
                    SELECT product.sku,
                        SUM(FORMAT(reservation.quantity, 0) *-1) AS 'reserved'
                    FROM catalog_product_entity AS product
                    JOIN inventory_reservation AS reservation ON reservation.sku = product.sku
                    GROUP BY product.sku
                ) AS reservation ON reservation.sku = product.sku
                WHERE product.sku IS NOT NULL;";

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        res = new BadRequestObjectResult("Exception report could not be generated");
                    } else
                    {
                        while (reader.Read())
                        {
                            results += reader.GetString(0) + ", ";
                            results += reader.GetString(1) + ", ";
                            results += reader.GetString(2) + "\n";
                        }
                    }
                }

                File.WriteAllText(path, results);

                res = new ObjectResult(results);
            }

            return res;
        }
    }
}
