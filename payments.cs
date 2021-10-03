using Magento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace magestack{

	public class Payments
	{
		private readonly Magestack _server;

		public Payments(Magestack server)
		{
			_server = server;
		}

		[FunctionName("payments")]
		public async Task<JsonResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "payment/{orderNum}")] HttpRequest req,
			string orderNum
            )
        {
			Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
			string qry = "SELECT increment_id, entity_id, shipping_description " +
				"FROM sales_order " +
				$"WHERE increment_id=\"{orderNum}\"";

			using (MySqlDataReader reader = await _server.ExecuteMySqlCommand(qry))
            {
				while (reader.Read())
                {
					Dictionary<string, string> values = new Dictionary<string, string>
					{
						{ "entity_id", reader.GetString("entity_id") },
						{ "shipping", reader.GetString("shipping_description") }
					};
					results.Add(reader.GetString("increment_id"), values);
                }
            }

			return new JsonResult(results);
        }
	}
}
