﻿using Magento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Collections.Generic;


namespace magestack
{
    public class MySqlMagento
    {
        private readonly Magestack _server;

        public MySqlMagento(Magestack server)
        {
            _server = server;
        }

        [FunctionName("orders")]
        public JsonResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderNum?}")] HttpRequest req,
            string orderNum,
            ILogger log)
        {   
            Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
            string qry = "SELECT increment_id, created_at, base_grand_total " +
                "FROM golfdi_mage2.sales_order ";

            if (orderNum != null)
            {
                qry += $"WHERE increment_id=\"{orderNum}\" ";
            }

            qry += "ORDER BY created_at " +
                "DESC LIMIT 10;";

            using (MySqlDataReader reader = _server.ExecuteMySqlCommand(qry))
            {
                while (reader.Read())
                {
                    Dictionary<string, string> values = new Dictionary<string, string>();
                    values.Add("created_at", reader.GetString("created_at"));
                    values.Add("base_grand_total", reader.GetString("base_grand_total"));
                    results.Add(reader.GetString("increment_id"), values);
                }
            }
            return new JsonResult(results);
        }
    }
}
