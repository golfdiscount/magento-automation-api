using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Renci.SshNet;


namespace magestack
{
    public static class Orders
    {
        [FunctionName("orders")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var httpClient = new HttpClient();
            var httpResult = await httpClient.GetAsync("https://api4.my-ip.io/ip");
            log.LogInformation(await httpResult.Content.ReadAsStringAsync());

            Magestack server = new Magestack();
            string result = server.ExecuteCmd("cd var/export/mmexportcsv && ls | grep PT_WSI_09_24");
            server.CloseCxn();

            return new OkObjectResult("Current directory listing:\n" + result);
        }
    }

    class Magestack
    {
        private readonly SshTunnel ssh;

        public Magestack()
        {
            ssh = new SshTunnel(Environment.GetEnvironmentVariable("stack_host"),
                3022,
                Environment.GetEnvironmentVariable("stack_user"),
                Environment.GetEnvironmentVariable("stack_pass"));
        }

        public string ExecuteCmd(string cmd)
        {
            return ssh.ExecuteCommand(cmd);
        }

        public void CloseCxn()
        {
            ssh.Disconnect();
        }
    }

    class SshTunnel
    {
        private readonly SshClient client;

        public SshTunnel(string host, int port, string user, string pass)
        {
            client = new SshClient(host, port, user, pass);
            client.Connect();
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public string ExecuteCommand(string cmd)
        {
            var command = client.CreateCommand(cmd);
            command.Execute();
            return command.Result;
        }
    }
}
