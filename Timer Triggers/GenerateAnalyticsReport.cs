using magestack.Components;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using QuestPDF.Fluent;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace magestack.Timer_Triggers
{
    public class GenerateAnalyticsReport
    {
        private readonly string cs;
        private readonly DataSet dataSet;
        public GenerateAnalyticsReport(string cs)
        {
            this.cs = cs;
            dataSet = new DataSet();
        }

        [FunctionName("GenerateAnalyticsReport")]
        public async Task Run([TimerTrigger("0 0 15 * * 1")]TimerInfo myTimer, ILogger log)
        {
            using (MySqlConnection conn = new MySqlConnection(cs))
            {
                string cmdText = @"SELECT DATE(created_at) AS `Date`,
                        COUNT(*) AS `Count`, 
                        FORMAT(SUM(grand_total), 2) AS `Grand Total`
                    FROM sales_order 
                    WHERE DATE(created_at) >= SUBDATE(CURDATE(), INTERVAL 1 WEEK)
                        AND DATE(created_at) < DATE(CURDATE())
                    GROUP BY DATE(created_at)
                    ORDER BY DATE(created_at) DESC;";
                FetchData(conn, cmdText, "Daily Orders Across All Sales Channels");

                cmdText = @"SELECT DATE(created_at) AS `Date`,
                        COUNT(*) AS `Count`,
                        FORMAT(SUM(grand_total), 2) AS `Grand Total`
                    FROM sales_order
                    WHERE increment_id LIKE '100%'
                        AND DATE(created_at) >= SUBDATE(CURDATE(), INTERVAL 1 WEEK)
                        AND DATE(created_at) < DATE(CURDATE())
                    GROUP BY DATE(created_at)
                    ORDER BY DATE(created_at) DESC;";
                FetchData(conn, cmdText, "Golfdiscount.com Orders for the Past Week");

                cmdText = @"SELECT top_skus.sku AS `SKU`,
                        product_varchar.`value` AS `Product Name`,
	                    top_skus.`units` AS `Units Ordered`
                    FROM(SELECT sku, FORMAT(SUM(qty_ordered), 0) AS `units`
                        FROM sales_order_item
                        WHERE DATE(created_at) >= SUBDATE(DATE(CURDATE()), INTERVAL 1 WEEK)
                            AND DATE(created_at) < DATE(CURDATE())
                        GROUP BY sku
                        ORDER BY SUM(qty_ordered) DESC
                        LIMIT 10) AS top_skus
                    JOIN catalog_product_entity AS product ON product.sku = top_skus.sku
                    JOIN catalog_product_entity_varchar AS product_varchar ON product_varchar.entity_id = product.entity_id
                    WHERE product_varchar.attribute_id = (SELECT attribute_id
                        FROM eav_attribute
                        WHERE eav_attribute.attribute_code = 'name'
                            AND eav_attribute.entity_type_id = (SELECT entity_type_id
                                FROM eav_entity_type
                                WHERE entity_type_code = 'catalog_product'));";
                FetchData(conn, cmdText, "Top 10 Products By Quantity Ordered");

                cmdText = @"SELECT top_skus.sku AS `SKU`,
	                    product_varchar.`value` AS `Product Name`,
                        top_skus.`Units Ordered`,
	                    top_skus.`$ Total`
                    FROM(SELECT sku, 
		                    FORMAT(SUM(row_total), 2) AS `$ Total`,
                            FORMAT(SUM(qty_ordered), 0) AS `Units Ordered`
	                    FROM sales_order_item
	                    WHERE DATE(created_at) >= SUBDATE(DATE(CURDATE()), INTERVAL 1 WEEK)
		                    AND DATE(created_at) < DATE(CURDATE())
	                    GROUP BY sku
	                    ORDER BY SUM(row_total) DESC
	                    LIMIT 10) AS top_skus
                    JOIN catalog_product_entity AS product ON product.sku = top_skus.sku
                    JOIN catalog_product_entity_varchar AS product_varchar ON product_varchar.entity_id = product.entity_id
                    WHERE product_varchar.attribute_id = (SELECT attribute_id
	                    FROM eav_attribute
	                    WHERE eav_attribute.attribute_code = 'name'
		                    AND eav_attribute.entity_type_id = (SELECT entity_type_id
			                    FROM eav_entity_type
			                    WHERE entity_type_code = 'catalog_product'));";
                FetchData(conn, cmdText, "Top 10 Products By $ Sold");

                cmdText = @"SELECT top_skus.`SKU`,
	                    product_varchar.`value` AS `Product Name`,
                        top_skus.`units` AS `Units Ordered`
                    FROM (SELECT sales_order_item.`sku` AS `SKU`,
		                    FORMAT(SUM(sales_order_item.`qty_ordered`), 0) AS `units`
	                    FROM sales_order
	                    JOIN sales_order_item ON sales_order_item.order_id = sales_order.entity_id
	                    WHERE increment_id LIKE '%%%-%'
		                    AND DATE(sales_order.`created_at`) >= SUBDATE(CURDATE(), INTERVAL 1 WEEK)
		                    AND DATE(sales_order.`created_at`) < DATE(CURDATE())
	                    GROUP BY sales_order_item.`sku`
	                    ORDER BY COUNT(*) DESC
	                    LIMIT 10) AS top_skus
                    JOIN catalog_product_entity AS product ON product.sku = top_skus.sku
                    JOIN catalog_product_entity_varchar AS product_varchar ON product_varchar.entity_id = product.entity_id
                    WHERE product_varchar.attribute_id = (SELECT attribute_id
                        FROM eav_attribute
                        WHERE eav_attribute.attribute_code = 'name'

                            AND eav_attribute.entity_type_id = (SELECT entity_type_id
                                FROM eav_entity_type
                                WHERE entity_type_code = 'catalog_product'));";
                FetchData(conn, cmdText, "Amazon Top 10 Products by Quantity Sold");

                cmdText = @"SELECT top_skus.`SKU`,
	                    product_varchar.`value` AS `Product Name`,
                        top_skus.`total` AS `$ Total`,
                        top_skus.`units` AS `Units Ordered`
                    FROM (SELECT sales_order_item.`sku` AS `SKU`,
		                    FORMAT(SUM(sales_order_item.`row_total`), 2) AS `total`,
		                    FORMAT(SUM(sales_order_item.`qty_ordered`), 0) AS `units`
	                    FROM sales_order
	                    JOIN sales_order_item ON sales_order_item.order_id = sales_order.entity_id
	                    WHERE increment_id LIKE '%%%-%'
		                    AND DATE(sales_order.`created_at`) >= SUBDATE(CURDATE(), INTERVAL 1 WEEK)
		                    AND DATE(sales_order.`created_at`) < DATE(CURDATE())
	                    GROUP BY sales_order_item.`sku`
	                    ORDER BY COUNT(*) DESC
	                    LIMIT 10) AS top_skus
                    JOIN catalog_product_entity AS product ON product.sku = top_skus.sku
                    JOIN catalog_product_entity_varchar AS product_varchar ON product_varchar.entity_id = product.entity_id
                    WHERE product_varchar.attribute_id = (SELECT attribute_id
                        FROM eav_attribute
                        WHERE eav_attribute.attribute_code = 'name'
    	                    AND  eav_attribute.entity_type_id = (SELECT entity_type_id
			                    FROM eav_entity_type
		                        WHERE entity_type_code = 'catalog_product'));";
                FetchData(conn, cmdText, "Amazon Top 10 Products by $ Sold");
            }

            AnalyticsDocument document = new AnalyticsDocument(dataSet);
            byte[] documentbytes = document.GeneratePdf();
            string base64Doc = Convert.ToBase64String(documentbytes);

            await SendReport(base64Doc);
        }

        public void FetchData(MySqlConnection conn, string cmdText, string name)
        {
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = cmdText;

                MySqlDataAdapter dataAdapter = new MySqlDataAdapter()
                {
                    SelectCommand = cmd
                };
                dataAdapter.Fill(dataSet, name);
                conn.Close();
            }
        }

        public static async Task SendReport(string base64Doc)
        {
            string sgApiKey = Environment.GetEnvironmentVariable("sendgrid_api");
            SendGridClient client = new SendGridClient(sgApiKey);

            DateTime today = DateTime.Today;

            string subject = "Pro Golf Analytics Report";
            string text = $"Report generated on {today:MM-dd-yyyy)}";


            EmailAddress from = new EmailAddress("harmeet@golfdiscount.com");

            string emailList = Environment.GetEnvironmentVariable("email_list");
            string[] emails = emailList.Split(',');
            List<EmailAddress> recipients = new List<EmailAddress>();

            foreach (string email in emails)
            {
                recipients.Add(new EmailAddress(email));
            }

            SendGridMessage msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, recipients, subject, text, "");
            msg.AddAttachment($"Analytics_{today:MM_dd_yyyy)}.pdf", base64Doc);
            await client.SendEmailAsync(msg).ConfigureAwait(false);
        }
    }
}
