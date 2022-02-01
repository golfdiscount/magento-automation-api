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
    /// Trigger for getting list of orders in processing
    /// </summary>
    public class GetProcessingOrders
    {
        private readonly MagentoDb _db;

        /// <summary>
        /// Initializes the trigger
        /// </summary>
        /// <param name="db"> Connection to the Magento database</param>
        public GetProcessingOrders(MagentoDb db)
        {
            _db = db;
        }

        /// <summary>
        /// Triggers a run of function
        /// </summary>
        /// <param name="req"> Http Request containing request information</param>
        /// <param name="log"> Logger object </param>
        /// <returns></returns>
        [FunctionName("getProcessingOrders")]
        public JsonResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string startDate = req.Query["startDate"].ToString();
            string endDate = req.Query["endDate"].ToString();

            log.LogInformation(startDate);
            log.LogInformation(endDate);

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