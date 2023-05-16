using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Pgd.Magento.Models.Wsi;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pgd.Magento;

/// <summary>
/// An instance of a timer trigger to upload files to WSI
/// </summary>
public class UploadWsiOrders
{
    private const string EXPORT_PATH = "/home/jetrails/golfdiscount.com/html/var/export/mmexportcsv";
    private readonly ConnectionInfo _sftpConnectionInfo;
    private readonly HttpClient _wsiClient;

    public UploadWsiOrders(ConnectionInfo sftpConnectionInfo, IHttpClientFactory clientFactory)
    {
        _sftpConnectionInfo = sftpConnectionInfo;
        _wsiClient = clientFactory.CreateClient("wsi");
    }

    [FunctionName("UploadWsiOrders")]
    [Singleton]
    public async Task Run(
        [TimerTrigger("0 9,11,21 * * *")]TimerInfo myTimer,
        ILogger log)
    {
        SftpClient sftp = new(_sftpConnectionInfo);
        try
        {
            sftp.Connect();
            log.LogInformation($"Searching for WSI order files...");

            if (sftp.WorkingDirectory != EXPORT_PATH) sftp.ChangeDirectory(EXPORT_PATH);

            IEnumerable<SftpFile> files = sftp.ListDirectory(sftp.WorkingDirectory);

            // Files containing WSI orders and should be archived after successful upload
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
                    string csvContents = sftp.ReadAllText(file.FullName).Trim();
                    IEnumerable<PickTicketModel> pickTickets = ParseCsv(csvContents);

                    foreach (PickTicketModel pickTicket in pickTickets)
                    {
                        log.LogInformation($"Submitting {pickTicket.PickTicketNumber}");

                        HttpRequestMessage request = new(HttpMethod.Post, "api/picktickets")
                        {
                            Content = JsonContent.Create(pickTicket)
                        };

                        HttpResponseMessage response = await _wsiClient.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            log.LogError($"Unable to create pick ticket {pickTicket.PickTicketNumber}: {response.ReasonPhrase}");
                        } else
                        {
                            log.LogInformation($"Created {pickTicket.PickTicketNumber}");
                        }
                    }

                } else if (noUpdateRgx.IsMatch(file.Name) && !file.IsDirectory)
                {
                    noUpdateFiles.Add(file);
                }
            }


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
        catch
        {
            throw;
        }
        finally
        {
            sftp.Disconnect();
        }
    }

    /// <summary>
    /// Parses a CSV containg pickticket headers and details
    /// </summary>
    /// <param name="csv">CSV to parse</param>
    /// <returns>A key value of pair of order number to an OrderModel</returns>
    private static IEnumerable<PickTicketModel> ParseCsv(string csv)
    {
        Dictionary<string, PickTicketModel> orders = new();
        string[] records = csv.Split("\n");

        foreach (string record in records)
        {
            string[] fields = record.Split(',');
            string recordType = fields[0];
            string pickticketNum = fields[2];

            if (!orders.ContainsKey(pickticketNum))
            {
                orders.Add(pickticketNum, new());
                orders[pickticketNum].PickTicketNumber = pickticketNum;
                orders[pickticketNum].LineItems = new();
                orders[pickticketNum].Channel = 1;
            }

            PickTicketModel order = orders[pickticketNum];

            if (recordType == "PTH")
            {
                order.OrderNumber = fields[3];
                order.OrderDate = DateTime.ParseExact(fields[5], "MM/dd/yyyy", null);
                order.Customer = new()
                {
                    Name = fields[12].Replace("\"", ""),
                    Street = fields[13].Replace("\"", ""),
                    City = fields[14].Replace("\"", ""),
                    State = fields[15],
                    Country = fields[16],
                    Zip = fields[17]
                };
                order.Recipient = new()
                {
                    Name = fields[19].Replace("\"", ""),
                    Street = fields[20].Replace("\"", ""),
                    City = fields[21].Replace("\"", ""),
                    State = fields[22],
                    Country = fields[23],
                    Zip = fields[24]
                };
                order.ShippingMethod = fields[32];
                order.Store = 1;
            }
            else if (recordType == "PTD")
            {
                order.LineItems.Add(new()
                {
                    Sku = fields[5],
                    Units = int.Parse(fields[10]),
                    LineNumber = int.Parse(fields[3])
                });
            }
        }

        return orders.Values;
    }
}