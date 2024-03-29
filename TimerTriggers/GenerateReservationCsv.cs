using Azure.Security.KeyVault.Secrets;
using FluentFTP;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Pgd.Magento.TimerTriggers
{
    public class GenerateReservationCsv
    {
        private readonly string cs;
        private readonly DataSet dataSet;
        private readonly SecretClient secretClient;
        
        public GenerateReservationCsv(string cs, SecretClient secretClient)
        {
            this.cs = cs;
            dataSet = new DataSet();
            this.secretClient = secretClient;
        }

        [FunctionName("GenerateWsiReservationCsv")]
        public void Run([TimerTrigger("0 0 */3 * * *")]TimerInfo myTimer, ILogger log)
        {
            using (MySqlConnection conn = new(cs))
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                string cmdText = @"SELECT product.`sku` AS `SKU`,
	                    FORMAT(inventory.`qty`, 0) AS `Quantity`,
                        FORMAT(reservation.`quantity`, 0) AS `Reserved Quantity`,
                        inventory.`stock_status` AS `Stock Status`,
                        product_int.`value` AS `Enabled Status`
                    FROM catalog_product_entity AS product
                    JOIN cataloginventory_stock_status AS inventory ON inventory.`product_id` = product.`entity_id`
                    JOIN catalog_product_entity_int AS product_int ON product_int.`entity_id` = product.`entity_id`
                    JOIN  inventory_reservation AS reservation ON reservation.`sku` = product.`sku`
                    WHERE inventory.`qty` > 0
	                    AND product.`sku` REGEXP '^[0-9]+$'
                        AND product_int.`attribute_id` = (SELECT `attribute_id`
		                    FROM eav_attribute
		                    WHERE `entity_type_id` = (SELECT `entity_type_id`
			                    FROM eav_entity_type
			                    WHERE `entity_type_code` LIKE 'catalog_product')
			                    AND `attribute_code` LIKE 'status');";
                cmd.CommandText = cmdText;
                conn.Open();

                using MySqlDataAdapter dataAdapter = new()
                {
                    SelectCommand = cmd
                };

                dataAdapter.Fill(dataSet, "Reservation");
                conn.Close();
            }

            DataTable reservations = dataSet.Tables["Reservation"];

            StringBuilder sb = new();

            for (int i = 0; i < reservations.Columns.Count; i++)
            {
                sb.Append(reservations.Columns[i]);
                if (i < reservations.Columns.Count - 1)
                {
                    sb.Append(',');
                }
            }

            sb.AppendLine();

            foreach (DataRow row in reservations.Rows)
            {
                for (int i = 0; i < reservations.Columns.Count; i++)
                {
                    sb.Append(row[i].ToString());
                    if (i < reservations.Columns.Count - 1)
                    {
                        sb.Append(',');
                    }
                }

                sb.AppendLine();
            }

            KeyVaultSecret host = secretClient.GetSecret("duffers-host");
            KeyVaultSecret user = secretClient.GetSecret("duffers-user");
            KeyVaultSecret pass = secretClient.GetSecret("duffers-pass");

            FtpClient ftp = new(host.Value, user.Value, pass.Value);
            ftp.Connect();

            byte[] fileContent = Encoding.UTF8.GetBytes(sb.ToString());
            ftp.UploadBytes(fileContent, "dufferscorner.com/media/reservation.csv");
        }
    }
}
