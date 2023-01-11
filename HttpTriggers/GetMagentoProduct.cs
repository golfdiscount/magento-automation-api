using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using StackExchange.Redis;
using System;
using System.Text;
using System.Text.Json;
using Pgd.Magento.Models;
using Pgd.Magento.Data;

namespace Pgd.Magento.HttpTriggers;

public class GetMagentoProduct
{
    private readonly string cs;
    private readonly IDatabase _redisDb;
    public GetMagentoProduct(string cs, ConnectionMultiplexer redis)
    {
        this.cs = cs;
        _redisDb = redis.GetDatabase();
    }

    [FunctionName("Products")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{sku}")] HttpRequest req,
        string sku,
        ILogger log)
    {
        log.LogInformation($"Searching for {sku} in Redis cache");

        try
        {
            string productInfo = _redisDb.StringGet(sku);

            if (productInfo != null)
            {
                log.LogInformation($"Found {sku} in Redis cache");
                return new JsonResult(JsonSerializer.Deserialize<ProductModel>(productInfo));
            }
        }
        catch
        {
            log.LogError($"Unable to query cache");
        }

        log.LogWarning($"Unable to find {sku} in cache, searching in database");

        using MySqlConnection conn = new(cs);
        conn.Open();
        ProductModel product = Products.GetProduct(sku, conn);

        if (product == null)
        {
            return new NotFoundObjectResult($"{sku} was not found in Magento");
        }

        QueueProduct(product);
        return new OkObjectResult(product);
        
    }

    private static void QueueProduct(ProductModel product)
    {
        string productInfo = JsonSerializer.Serialize(product);
        productInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(productInfo));
        QueueClient client = new(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "cache-products");
        client.SendMessage(productInfo);
    }
}
