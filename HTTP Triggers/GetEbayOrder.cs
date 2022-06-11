using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace magestack.routes
{
    public class GetEbayOrder
    {
        private readonly string _cs;
        public GetEbayOrder(string cs)
        {
            _cs = cs;
        }

        [FunctionName("GetEbayOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ebay/orders/{orderId}")] HttpRequest req,
            string orderId,
            ILogger log)
        {
            IActionResult res;
            log.LogInformation($"Looking for an eBay order with eBay order ID: {orderId}");

            using (MySqlConnection cxn = new MySqlConnection(_cs))
            using (MySqlCommand cmd = cxn.CreateCommand())
            {
                cxn.Open();
                cmd.CommandText = $@"SELECT sales_order.entity_id AS 'id',
                    state,
                    `status`,
                    shipping_description AS 'shipping',
                    customer_id,
                    billing_address_id,
                    FORMAT(sales_order.base_grand_total, 2) AS 'total',
                    FORMAT(sales_order.base_shipping_amount, 2) AS 'ship_cost',
                    created_at,
                    updated_at,
                    increment_id AS 'order_number',
                    payment.method
                FROM sales_order
                INNER JOIN m2epro_order ON magento_order_id = sales_order.entity_id
                INNER JOIN m2epro_ebay_order ON order_id = m2epro_order.id
                JOIN sales_order_payment AS payment ON payment.parent_id = sales_order.entity_id
                WHERE ebay_order_id = '{orderId}';";

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    res = new NotFoundObjectResult("Order could not be found in Magento");
                }
                else
                {
                    Dictionary<string, string> result = new Dictionary<string, string>();

                    while (reader.Read())
                    {
                        result.Add("id", GetOrdinalValue(reader, 0));
                        result.Add("state", GetOrdinalValue(reader, 1));
                        result.Add("status", GetOrdinalValue(reader, 2));
                        result.Add("shipping", GetOrdinalValue(reader, 3));
                        result.Add("customer_id", GetOrdinalValue(reader, 4));
                        result.Add("billing_address_id", GetOrdinalValue(reader, 5));
                        result.Add("total", GetOrdinalValue(reader, 6));
                        result.Add("ship_cost", GetOrdinalValue(reader, 7));
                        result.Add("created_at", GetOrdinalValue(reader, 8));
                        result.Add("updated_at", GetOrdinalValue(reader, 9));
                        result.Add("order_number", GetOrdinalValue(reader, 10));
                        result.Add("payment_method", GetOrdinalValue(reader, 11));
                    }

                    res = new OkObjectResult(result);
                }
            }

                return res;
        }

        private string GetOrdinalValue(MySqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }
    }
}
