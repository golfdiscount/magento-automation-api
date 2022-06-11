using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.IO;
using System.Threading.Tasks;

namespace magestack.routes
{
    public class UploadEagleOrder
    {
        private readonly SftpClient _sftp;
        private const string rootDirectory = "/microcloud/domains/golfdi/domains/golfdiscount.com/http/";
        public UploadEagleOrder(SftpClient sftp)
        {
            _sftp = sftp;
        }

        [FunctionName("UploadEagleOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "eagle/{filename}")] HttpRequest req,
            string filename, ILogger log)
        {
            if (!_sftp.IsConnected)
            {
                _sftp.Connect();
            }

            log.LogInformation($"Uploading {filename} to {_sftp.WorkingDirectory}");

            using (Stream reqContents = req.Body)
            {
                byte[] byteContent = new byte[reqContents.Length];
                reqContents.Read(byteContent, 0, byteContent.Length);
                _sftp.WriteAllBytes($"{rootDirectory}/{Environment.GetEnvironmentVariable("eagle_files")}/{filename}", byteContent);
            }

            _sftp.Disconnect();
            return new OkObjectResult("Received");
        }
    }
}
