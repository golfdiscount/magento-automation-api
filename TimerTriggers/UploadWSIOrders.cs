using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pgd.Magento;

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

            // These are files that indicate there were no orders to generate at that time stamp
            // and should be deleted
            List<SftpFile> noUpdateFiles = new();

            foreach (SftpFile file in files)
            {
                Regex ptRgx = new($"PT_WSI");
                Regex noUpdateRgx = new($"PT_NO_UPDATE");

                if (ptRgx.IsMatch(file.Name) && !file.IsDirectory)
                {
                    wsiFiles.Add(file);
                } else if (noUpdateRgx.IsMatch(file.Name) && !file.IsDirectory)
                {
                    noUpdateFiles.Add(file);
                }
            }

            if (wsiFiles.Count != 0)
            {
                log.LogInformation($"Found {wsiFiles.Count} WSI file(s)");
                log.LogInformation("Joining files");
                List<byte> fileBytes = new();

                foreach (SftpFile file in wsiFiles)
                {
                    fileBytes.AddRange(sftp.ReadAllBytes(file.FullName));
                }

                HttpRequestMessage requestMessage = new(HttpMethod.Post, "api/picktickets")
                {
                    Content = new ByteArrayContent(fileBytes.ToArray())
                };
                requestMessage.Content.Headers.ContentType = new("text/csv");
                HttpResponseMessage response = await wsiClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                log.LogInformation("File sent to WSI successfully");
            }

            // Move PT_WSI files to the PT_archive directory
            // Delete any PT_NO_UPDATE files
            if (bool.Parse(Environment.GetEnvironmentVariable("ArchiveFiles")))
            {
                log.LogInformation($"Archiving {wsiFiles.Count} WSI files");
                foreach (SftpFile file in wsiFiles)
                {
                    log.LogInformation($"Archiving {file.Name}");
                    file.MoveTo($"{sftp.WorkingDirectory}/PT_archive/{file.Name}");
                }

                log.LogInformation($"Deleting {noUpdateFiles.Count} PT_NO_UPDATE files");
                foreach (SftpFile file in noUpdateFiles)
                {
                    log.LogInformation($"Deleting {file.Name}");
                    file.Delete();
                }
            }
        } 
        catch
        {
            throw;
        }
        finally
        {
            sftp.Disconnect();
        }
    }
}