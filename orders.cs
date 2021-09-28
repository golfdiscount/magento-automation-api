using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Magento;
using Renci;


namespace magestack
{
    public class Orders
    {
        [FunctionName("WsiOrders")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            log.LogInformation("Connecting to server");
            Magestack server = new Magestack();
            SftpClient sftp = server.CreateSftpClient();

            sftp.ChangeDir("var/export/mmexportcsv");
            List<Renci.SshNet.Sftp.SftpFile> files = sftp.List();

            string result = "";
            foreach (Renci.SshNet.Sftp.SftpFile file in files)
            {
                result += file.Name + "\n";
            }


            log.LogInformation("Disconnecting from server");
            server.Disconnect();
            return new OkObjectResult("Current directory listing:\n" + result);
        }
    } 
}
