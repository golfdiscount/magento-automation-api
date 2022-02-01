using Magento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Collections.Generic;


namespace magestack
{
    /// <summary>
    /// Trigger for getting order information
    /// </summary>
    public class MySqlMagento
    {
        private readonly MagentoDb _db;

        /// <summary>
        /// Initializes the trigger
        /// </summary>
        /// <param name="db"> Connection to Magento database</param>
        public MySqlMagento(MagentoDb db)
        {
            _db = db;
        }

        /// <summary>
        /// Triggers a run of the function
        /// </summary>
        /// <param name="req"> Http Reqeust object containing request information </param>
        /// <param name="orderNum"> Order number to search for </param>
        /// <param name="log"> Logger object </param>
        /// <returns></returns>
        [FunctionName("orders")]
        public JsonResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderNum?}")] HttpRequest req,
            string orderNum,
            ILogger log)
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            string qry = "SELECT increment_id, created_at, base_grand_total, customer_firstname, customer_lastname " +
                "FROM golfdi_mage2.sales_order ";

            if (orderNum != null)
            {
                qry += $"WHERE increment_id=\"{orderNum}\" ";
            }

            qry += "ORDER BY created_at " +
                "DESC LIMIT 10;";

            using (MySqlDataReader reader = _db.ExecuteDbCommand(qry))
            {
                while (reader.Read())
                {
                    Dictionary<string, string> values = new Dictionary<string, string>()
                    {
                        { "increment_id", reader.GetString("increment_id") }, 
                        { "created_at", reader.GetString("created_at") },
                        { "base_grand_total", reader.GetString("base_grand_total") },
                        { "customer_firstname", reader.GetString("customer_firstname") },
                        { "customer_lastname", reader.GetString("customer_lastname") }
                        
                    };

                    results.Add(values);
                }
            }
            return new JsonResult(results);
        }
    }
}
