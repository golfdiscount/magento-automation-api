using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Azure;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace magestack
{
    /// <summary>
    /// An instance of a timer trigger to upload files to WSI
    /// </summary>
    public class UploadWsiOrders
    {
        private const string EXPORT_PATH = "/microcloud/domains/golfdi/domains/golfdiscount.com/http/var/export/mmexportcsv";
        private readonly SftpClient sftp;
        private readonly HttpClient wsiClient;

        public UploadWsiOrders(SftpClient sftp, IHttpClientFactory clientFactory)
        {
            this.sftp = sftp;
            wsiClient = clientFactory.CreateClient("wsi");
        }

        [FunctionName("UploadWsiOrders")]
        [Singleton]
        public async Task Run(
            [TimerTrigger("45 08,20 * * *")]TimerInfo myTimer,
            ILogger log)
        {
            sftp.Connect();

            try
            {
                log.LogInformation($"Searching for WSI order files...");

                if (sftp.WorkingDirectory != EXPORT_PATH) sftp.ChangeDirectory("var/export/mmexportcsv");

                IEnumerable<SftpFile> files = sftp.ListDirectory(sftp.WorkingDirectory);
                List<SftpFile> wsiFiles = new();

                foreach (SftpFile file in files)
                {
                    Regex rgx = new($"PT_WSI_{string.Format("{0:MM_dd_yyy}", DateTime.Today)}");

                    if (rgx.IsMatch(file.Name) && !file.IsDirectory)
                    {
                        wsiFiles.Add(file);
                    }
                }

                if (wsiFiles.Count != 0)
                {
                    log.LogInformation($"Found {wsiFiles.Count} WSI file(s) for {string.Format("{0:MM/dd/yyy}", DateTime.Today)}");
                    log.LogInformation("Joining files");
                    List<byte> fileBytes = new();

                    foreach (SftpFile file in wsiFiles)
                    {
                        fileBytes.AddRange(sftp.ReadAllBytes(file.FullName));
                    }

                    HttpRequestMessage requestMessage = new(HttpMethod.Post, "api/orders");
                    requestMessage.Content = new ByteArrayContent(fileBytes.ToArray());
                    requestMessage.Content.Headers.ContentType = new("text/csv");
                    HttpResponseMessage response = await wsiClient.SendAsync(requestMessage);
                    response.EnsureSuccessStatusCode();

                    log.LogInformation("File sent to WSI successfully");

                    if (bool.Parse(Environment.GetEnvironmentVariable("ArchiveFiles")))
                    {
                        foreach (SftpFile file in wsiFiles)
                        {
                            log.LogInformation($"Archiving {file.Name}");
                            file.MoveTo($"{sftp.WorkingDirectory}/PT_archive/{file.Name}");
                        }
                    }
                }
                else log.LogWarning("There were no WSI files to upload");
            } 
            catch (HttpRequestException e)
            {
                throw e;
            }
            finally
            {
                sftp.Disconnect();
            }
        }
    }
}