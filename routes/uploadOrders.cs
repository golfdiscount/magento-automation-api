using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace magestack
{
    /// <summary> Trigger object that uploads orders to WSI SFTP server </summary>
    public class WsiHttp
    {
        private readonly SftpClient _sftp;

        /// <summary> Initiates a trigger run to upload orders to WSI SFTP server </summary>
        /// <param name="sftp">SFTP client connected to Magento server</param>
        public WsiHttp(SftpClient sftp)
        {
            _sftp = sftp;
        }

        /// <summary> Runs the trigger </summary>
        /// <param name="req">Request to the HTTP endpoint</param>
        /// <param name="log">Logging middleware</param>
        /// <returns>HTTP response with results of uploading orders process</returns>
        [FunctionName("uploadOrders")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            _sftp.Connect();
            const string EXPORT_PATH = "/microcloud/domains/golfdi/domains/golfdiscount.com/http/var/export/mmexportcsv";
            string today = DateTime.Today.ToString("MM/dd/yyyy");
            log.LogInformation($"Looking for WSI order files for {today}...");

            if (_sftp.WorkingDirectory != EXPORT_PATH)
            {
                _sftp.ChangeDirectory("var/export/mmexportcsv");
            }

            IEnumerable<SftpFile> files = _sftp.ListDirectory(_sftp.WorkingDirectory);
            List<SftpFile> wsiFiles = new List<SftpFile>();

            foreach (SftpFile file in files)
            {
                Regex rgx = new Regex($"PT_WSI_{string.Format("{0:MM_dd_yyy}", DateTime.Today)}");

                if (rgx.IsMatch(file.Name) && !file.IsDirectory)
                {
                    wsiFiles.Add(file);
                }
            }

            if (wsiFiles.Count != 0)
            {
                log.LogInformation($"Found {wsiFiles.Count} WSI file(s) for {string.Format("{0:MM/dd/yyy}", DateTime.Today)}");
                log.LogInformation("Joining files");
                List<byte> fileBytes = new List<byte>();

                foreach(SftpFile file in wsiFiles)
                {
                    fileBytes.AddRange(_sftp.ReadAllBytes(file.FullName));
                }

                log.LogInformation("Uploading to WSI storage container");
                UploadToStorage(fileBytes.ToArray());

                foreach(SftpFile file in wsiFiles)
                {
                    log.LogInformation($"Archiving {file.Name}");
                    file.MoveTo($"{_sftp.WorkingDirectory}/PT_archive/{file.Name}");
                }
            } else
            {
                log.LogWarning("There were no WSI files to upload");
            }
            _sftp.Disconnect();
            return new OkObjectResult($"{wsiFiles.Count} file(s) processed and uploaded successfully");
        }

        /// <summary> Takes file contents and uploads them to a blob at WSI storage </summary>
        /// <param name="fileContent"><c>byte[]</c> of the file content</param>
        private void UploadToStorage(byte[] fileContent)
        {
            string fileName = Guid.NewGuid().ToString();
            BlobClient file = new BlobClient(Environment.GetEnvironmentVariable("wsi_storage"),
                "wsi-orders",
                fileName);
            file.Upload(new BinaryData(fileContent));
        }
    }
}