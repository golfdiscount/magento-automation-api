using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Renci.SshNet.Sftp;

namespace Magento
{
    public class SftpClient
    {
        private readonly Renci.SshNet.SftpClient client;

        public SftpClient(string host, int port, string user, string pass)
        {
            client = new Renci.SshNet.SftpClient(host, port, user, pass);
            Connect();
        }

        public void Connect()
        {
            client.Connect();
        }

        public void ChangeDir(string dir)
        {
            try
            {
                client.ChangeDirectory(dir);
            }
            catch (Renci.SshNet.Common.SftpPathNotFoundException)
            {
                throw new ArgumentException("Directory given is not valid");
            }
            catch (Renci.SshNet.Common.SshConnectionException)
            {
                Connect();
                client.ChangeDirectory(dir);
            }
        }

        public List<SftpFile> List(bool isDir = false, string pattern = null)
        {
            List<SftpFile> fileList = new List<SftpFile>();
            IEnumerable<SftpFile> files = client.ListDirectory(this.WorkingDirectory);

            foreach (SftpFile file in files)
            {
                if (pattern is null)
                {
                    if (file.IsDirectory == isDir)
                    {
                        fileList.Add(file);
                    }
                }
                else
                {
                    Regex rgx = new Regex(pattern);

                    if (rgx.IsMatch(file.Name) && file.IsDirectory == isDir)
                    {
                        fileList.Add(file);
                    }
                }
            }

            return fileList;
        }

        public byte[] ReadFile(SftpFile file)
        {
            return this.client.ReadAllBytes(file.FullName);
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public string WorkingDirectory
        {
            get { return client.WorkingDirectory; }
            set { ChangeDir(value); }
        }
    }
}
