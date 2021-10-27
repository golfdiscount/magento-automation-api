using Magento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace magestack{

	/// <summary> A Payments trigger </summary>
	public class Payments
	{
		private readonly MagentoDb _db;

		/// <summary> Initializes a Payments object</summary>
		/// <param name="db"> The connection to the Magento database</param>
		public Payments(MagentoDb db)
		{
			_db = db;
		}

		/// <summary> Triggers a run of the payments route </summary>
		/// <param name="req"> HttpRequest to this endpoint </param>
		/// <param name="orderNum"> Order number passed in as a route parameter </param>
		/// <returns></returns>
		[FunctionName("payments")]
		public JsonResult Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "payments/{orderNum}")] HttpRequest req,
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
