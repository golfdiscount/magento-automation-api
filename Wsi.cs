using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Magento;

namespace magestack
{
    public class Wsi
    {
        [FunctionName("listOrders")]
        public void Run([TimerTrigger("45 8 * * 1-5")]TimerInfo timer, ILogger log)
        {
            string today = DateTime.Today.ToString("MM/dd/yyyy");
            log.LogInformation($"Getting WSI order sheets for {today}");

            Magestack server = new Magestack();
            log.LogInformation(server.ConnectionInfo());
            SftpClient sftp = server.CreateSftpClient();
            sftp.ChangeDir("var/export/mmexportcsv");
            List<Renci.SshNet.Sftp.SftpFile> files = sftp.List(
                pattern: "PT_WSI_" + String.Format("{0:MM_dd_yyy}", DateTime.Today)
            );

            log.LogInformation("Current directory listing:");
            
            foreach (Renci.SshNet.Sftp.SftpFile file in files)
            {
                log.LogInformation(file.Name);
            }

            log.LogInformation("Disconnecting from server");
            server.Disconnect();
        }
    }
}