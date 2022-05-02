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
    public class getOrder
    {
        private readonly string _cs;

        public getOrder(string cs)
        {
            _cs = cs;
        }

        [FunctionName("getOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{order_num}")] HttpRequest req,
            string order_num,
            ILogger log)
        {
            IActionResult res;
            log.LogInformation($"Searching for {order_num} in the database");

            using (MySqlConnection cxn = new MySqlConnection(_cs))
            using (MySqlCommand cmd = cxn.CreateCommand())
            {
                cxn.Open();
                cmd.CommandText = @$"SELECT entity_id AS 'id',
                    state,
                    `status`,
                    shipping_description AS 'shipping',
                    customer_id,
                    billing_address_id,
                    FORMAT(base_grand_total, 2) AS 'total',
                    FORMAT(base_shipping_amount, 2) AS 'ship_cost',
                    created_at,
                    updated_at,
                    increment_id AS 'order_number'
                FROM sales_order
                WHERE increment_id = '{order_num}';";

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