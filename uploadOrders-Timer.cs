using Magento;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace magestack
{
    public class WsiTimer
    {
        private readonly SftpClient _sftp;

        public WsiTimer(SftpClient sftp)
        {
            _sftp = sftp;
        }

        [FunctionName("uploadOrders-timer")]
        [Singleton]
        public async Task Run(
            [TimerTrigger("45 15 * * *")]TimerInfo myTimer,
            ILogger log)
        {
            string today = DateTime.Today.ToString("MM/dd/yyyy");
            log.LogInformation($"Looking for WSI order files for {today}...");

            _sftp.ChangeDir("var/export/mmexportcsv");
            List<Renci.SshNet.Sftp.SftpFile> files = _sftp.List(
                pattern: "PT_WSI_" + string.Format("{0:MM_dd_yyy}", DateTime.Today)
            );

            log.LogInformation($"Found {files.Count} WSI files");
            if (files.Count != 0)
            {
                log.LogInformation("Processing files");

                List<byte[]> fileByteArrays = ConvertFiles(files, log);
                log.LogInformation("Uploading to WSI API");

                HttpClient requester = new HttpClient();
                // Uploading to WSI can take a while, timeout is set to 5 minutes
                requester.Timeout = new TimeSpan(0, 5, 0);

                foreach (byte[] file in fileByteArrays)
                {
                    await requester.PostAsync(Environment.GetEnvironmentVariable("wsi_url"), new ByteArrayContent(file));
                }
            } else
            {
                log.LogWarning("There were no WSI files to upload");
            }
        }

        private List<byte[]> ConvertFiles(List<Renci.SshNet.Sftp.SftpFile> files, ILogger log)
        {
            string result = "";
            List<byte[]> fileByteArrays = new List<byte[]>();
            foreach (Renci.SshNet.Sftp.SftpFile file in files)
            {
                log.LogInformation($"Writing {file.Name}");
                // Byte array of file contents
                byte[] fileContents = _sftp.ReadFile(file);

                fileByteArrays.Add(fileContents);

                // Concatenated value of byte array (a singular file)
                string records = "";
                foreach (byte value in fileContents)
                {
                    records += ((char)value).ToString();
                }

                result += records;
            }

            return fileByteArrays;
        }
    }
}