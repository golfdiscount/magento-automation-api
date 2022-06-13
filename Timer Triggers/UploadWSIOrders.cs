﻿using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace magestack
{
    /// <summary>
    /// An instance of a timer trigger to upload files to WSI
    /// </summary>
    public class UploadWsiOrders
    {
        private readonly SftpClient sftp;

        public UploadWsiOrders(SftpClient sftp)
        {
            this.sftp = sftp;
        }

        [FunctionName("UploadWsiOrders")]
        [Singleton]
        public void Run(
            [TimerTrigger("45 15,03 * * *")]TimerInfo myTimer,
            ILogger log)
        {
            sftp.Connect();
            const string EXPORT_PATH = "/microcloud/domains/golfdi/domains/golfdiscount.com/http/var/export/mmexportcsv";
            string today = DateTime.Today.ToString("MM/dd/yyyy");
            log.LogInformation($"Looking for WSI order files for {today}...");

            if (sftp.WorkingDirectory != EXPORT_PATH) sftp.ChangeDirectory("var/export/mmexportcsv");

            IEnumerable<SftpFile> files = sftp.ListDirectory(sftp.WorkingDirectory);
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

                foreach (SftpFile file in wsiFiles)
                {
                    fileBytes.AddRange(sftp.ReadAllBytes(file.FullName));
                }

                log.LogInformation("Uploading to WSI storage container");
                UploadToStorage(fileBytes.ToArray());

                foreach (SftpFile file in wsiFiles)
                {
                    log.LogInformation($"Archiving {file.Name}");
                    file.MoveTo($"{sftp.WorkingDirectory}/PT_archive/{file.Name}");
                }
            }
            else log.LogWarning("There were no WSI files to upload");

            sftp.Disconnect();
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