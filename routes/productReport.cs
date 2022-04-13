using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;

namespace magestack.routes
{
    public class productReport
    {
        private readonly string _cs;

        public productReport(String cs)
        {
            _cs = cs;
        }

        [FunctionName("GenerateExceptionReport")]
        public void Run(
            [TimerTrigger("0 * * * *")]TimerInfo myTimer,
            ILogger log)
        {
            log.LogInformation("Generating exception report from Magento...");

            using (MySqlConnection cxn = new MySqlConnection(_cs))
            using (MySqlCommand cmd = cxn.CreateCommand())
            {
                string results = "";
                cxn.Open();
                cmd.CommandText = $@"SELECT product.sku,
	                REPLACE(FORMAT(inventory.qty, 0), ',', '') AS 'quantity',
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
                    while (reader.Read())
                    {
                        results += reader.GetString(0) + ", ";
                        results += reader.GetString(1) + ", ";
                        results += reader.GetString(2) + "\n";
                    }
                }

                BlobClient file = new BlobClient(
                    Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                    "export",
                    "productReport");

                file.Upload(new BinaryData(results), true);
            }
        }
    }
}
