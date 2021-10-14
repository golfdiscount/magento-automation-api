using Magento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace magestack{

	public class Payments
	{
		private readonly MagentoDb _db;

		public Payments(MagentoDb db)
		{
			_db = db;
		}

		[FunctionName("payments")]
		public JsonResult Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "payment/{orderNum}")] HttpRequest req,
			string orderNum
            )
        {
			Dictionary<string, string> results = new Dictionary<string, string>();
			string qry = "SELECT increment_id, entity_id, shipping_description " +
				"FROM sales_order " +
				$"WHERE increment_id=\"{orderNum}\"";


			using (MySqlDataReader reader = _db.ExecuteDbCommand(qry))
            {
				while (reader.Read())
                {

					results.Add("increment_id", reader.GetString("increment_id"));
					results.Add("entity_id", reader.GetString("entity_id"));
					results.Add("shipping", reader.GetString("shipping_description"));
                }
            }
			return new JsonResult(results);
        }
	}
}
