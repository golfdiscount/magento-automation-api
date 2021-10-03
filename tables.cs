using Magento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;


namespace magestack
{
    public class MySqlMagento
    {
        [FunctionName("orders")]
        public JsonResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            Magestack server = new Magestack();
            SshTunnel ssh = server.CreateSshClient();
            // Create a tunnel with connection to DB bounded to 127.0.0.1:3307
            ssh.ForwardPort("127.0.0.1", 3307, Environment.GetEnvironmentVariable("db_host"), uint.Parse(Environment.GetEnvironmentVariable("db_port")));
            // Create a connection to the DB that was previously bounded to 127.0.0.1:3307
            server.CreateMySqlConn("127.0.0.1", 3307, Environment.GetEnvironmentVariable("db_user"), Environment.GetEnvironmentVariable("db_pass"));
            MySqlDataReader reader = server.ExecuteMySqlCommand(
                "SELECT increment_id, created_at, base_grand_total " +
                "FROM golfdi_mage2.sales_order " +
                "ORDER BY created_at " +
                "DESC LIMIT 10; ");

            Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();

            while (reader.Read())
            {
                Dictionary<string, string> values = new Dictionary<string, string>();
                values.Add("created_at", reader.GetString("created_at"));
                values.Add("base_grand_total", reader.GetString("base_grand_total"));
                results.Add(reader.GetString("increment_id"), values);
            }

            reader.Close();
            server.Disconnect();

            return new JsonResult(results);
        }
    }
}
