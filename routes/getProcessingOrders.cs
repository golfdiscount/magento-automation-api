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
    public class getProcessingOrders
    {
        private readonly MagentoDb _db;

        public getProcessingOrders(MagentoDb db)
        {
            _db = db;
        }

        [FunctionName("getProcessingOrders")]
        public JsonResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            List<string> results = new List<string>();

            string qry = "SELECT increment_id FROM sales_order " +
                $"WHERE created_at >= (NOW() - INTERVAL 2 WEEK) " +
                "AND created_at <= (NOW() - INTERVAL 1 DAY) " +
                "AND  increment_id NOT LIKE \"5000%\"" + 
                "AND state = \"processing\" " +
                "ORDER BY created_at DESC;";

            using (MySqlDataReader reader = _db.ExecuteDbCommand(qry))
            {
                while (reader.Read())
                {
                    results.Add(reader.GetString("increment_id"));
                }
            }

            return new JsonResult(results);
        }
    }
}