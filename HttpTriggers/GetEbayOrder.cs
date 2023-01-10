using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Pgd.Magento.HttpTriggers;

/// <summary>
/// HTTP trigger for getting an Ebay order from the Magento database
/// </summary>
public class GetEbayOrder
{
    private readonly string cs;

    /// <summary>
    /// Constructor for the GetEbayOrders HTTP trigger
    /// </summary>
    /// <param name="cs">
    /// Connection string to the Magento MySQL database
    /// </param>
    public GetEbayOrder(string cs)
    {
        this.cs = cs;
    }

    /// <summary>
    /// Entry point for running the GetEbayOrders HTTP trigger
    /// </summary>
    /// <param name="req">
    /// HTTP request submitted to the functions runtime
    /// </param>
    /// <param name="orderId">
    /// The Magento order ID related to the eBay order, typically starts with 500
    /// </param>
    /// <param name="log">
    /// An instance of <c>ILogger</c> used for logging to the functions runtime
    /// </param>
    /// <returns>
    /// An instance of <c>IActionResult</c> with an HTTP result like
    /// <c>NotFoundObjectResult</c> or <c>OkObjectResult</c>
    /// </returns>
    [FunctionName("GetEbayOrders")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ebay/orders/{orderId}")] HttpRequest req,
        string orderId,
        ILogger log)
    {
        IActionResult res;
        log.LogInformation($"Looking for an eBay order with eBay order ID: {orderId}");

        using (MySqlConnection cxn = new(cs))
        using (MySqlCommand cmd = cxn.CreateCommand())
        {
            cxn.Open();
            cmd.CommandText = @"SELECT sales_order.entity_id AS 'id',
                    state,
                    `status`,
                    shipping_description AS 'shipping',
                    customer_id,
                    billing_address_id,
                    FORMAT(sales_order.base_grand_total, 2) AS 'total',
                    FORMAT(sales_order.base_shipping_amount, 2) AS 'ship_cost',
                    created_at,
                    updated_at,
                    increment_id AS 'order_number',
                    payment.method
                FROM sales_order
                INNER JOIN m2epro_order ON magento_order_id = sales_order.entity_id
                INNER JOIN m2epro_ebay_order ON order_id = m2epro_order.id
                JOIN sales_order_payment AS payment ON payment.parent_id = sales_order.entity_id
                WHERE ebay_order_id = @ebay_order_id;";

            cmd.Parameters.AddWithValue("@ebay_order_id", orderId);

            using MySqlDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                res = new NotFoundObjectResult("Order could not be found in Magento");
            }
            else
            {
                Dictionary<string, string> result = new();

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
                    result.Add("payment_method", GetOrdinalValue(reader, 11));
                }

                res = new OkObjectResult(result);
            }
        }

            return res;
    }

    /// <summary>
    /// Gets the value of a certain column ordinal (column number) in a <paramref name="reader"/>
    /// </summary>
    /// <param name="reader">
    /// A data reader with the results of a MySQL query
    /// </param>
    /// <param name="ordinal">
    /// Ordinal value of a column to get (0 gets the first column)
    /// </param>
    /// <returns></returns>
    private static string GetOrdinalValue(MySqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }
}
