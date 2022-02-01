using Magento;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string today = DateTime.Today.ToString("MM/dd/yyyy");
            log.LogInformation($"Looking for WSI order files for {today}...");

            if (_sftp.WorkingDirectory != "/microcloud/domains/golfdi/domains/golfdiscount.com/http/var/export/mmexportcsv")
            {
                _sftp.ChangeDir("var/export/mmexportcsv");
            }

            List<SftpFile> files = _sftp.List(
                pattern: "PT_WSI_" + string.Format("{0:MM_dd_yyy}", DateTime.Today)
            );
            log.LogInformation($"Found {files.Count} WSI file(s)");

            if (files.Count != 0)
            {
                log.LogInformation("Joining files");

                byte[] fileBytes = ConvertFiles(files, log);
                log.LogInformation("Uploading to WSI API");

                Console.WriteLine(fileBytes.ToString());

/*                HttpClient requester = new HttpClient {
                    Timeout = new TimeSpan(0, 5, 0)
                };

                HttpResponseMessage res = await requester.PostAsync(Environment.GetEnvironmentVariable("wsi_url"), 
                    new ByteArrayContent(fileBytes));

                if (res.IsSuccessStatusCode)
                {
                    foreach (SftpFile file in files)
                    {
                        log.LogInformation($"Successfully uploaded {file.Name}");
                        log.LogInformation($"Adding {file.Name} to recently uploaded files");
                        _sftp.TrackFile(file.Name);
                    }

                }
                else
                {
                    log.LogWarning($"There was an issue uploading the files, please check the logs");
                    string error = await res.Content.ReadAsStringAsync();
                    log.LogWarning(error);
                    return new BadRequestObjectResult($"There was an issue uploading the files: {error}");
                }*/
            } else
            {
                log.LogWarning("There were no WSI files to upload");
            }
            return new OkObjectResult($"{files.Count} file(s) processed and uploaded successfully");
        }

        /// <summary> Takes a list of files of converts them to a singular byte array </summary>
        /// <param name="files">List of file names to convert</param>
        /// <param name="log">Logging middleware to output progress</param>
        /// <returns><c>byte[]</c> associated with their file names</returns>
        private byte[] ConvertFiles(List<SftpFile> files, ILogger log)
        {
            List<byte> fileBytes = new List<byte>();
            foreach (SftpFile file in files)
            {
                if(!_sftp.Uploaded(file.Name))
                {
                    log.LogInformation($"Writing {file.Name}");
                    // Byte array of file contents
                    fileBytes.AddRange(_sftp.ReadFile(file));
                } else
                {
                    log.LogWarning($"{file.Name} has already been uploaded");
                }

            }

            return fileBytes.ToArray();
        }
    }
}