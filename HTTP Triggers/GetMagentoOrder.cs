using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using magestack.Data;
using magestack.Models;

namespace magestack.routes
{
    public class GetMagentoOrder
    {
        private readonly string cs;

        public GetMagentoOrder(string cs)
        {
            this.cs = cs;
        }

        [FunctionName("GetMagentoOrders")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{orderNumber}")] HttpRequest req,
            string orderNumber,
            ILogger log)
        {
            log.LogInformation($"Searching for {orderNumber} in the database");

            using MySqlConnection conn = new(cs);
            conn.Open();

            OrderModel order = Orders.GetOrder(orderNumber, conn);

            if (order == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(order);
        }
    }
}
