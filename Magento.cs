using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Magento
{
    class Magestack
    {
        private SshTunnel _ssh;
        private  SftpClient _sftp;
        private readonly string _host;
        private readonly string _user;
        private readonly string _pass;
        private readonly int _port = 3022;

        public Magestack()
        {
            _host = Environment.GetEnvironmentVariable("stack_host");
            _user = Environment.GetEnvironmentVariable("stack_user");
            _pass = Environment.GetEnvironmentVariable("stack_pass");
        }

        public SshTunnel CreateSshClient()
        {
            _ssh = new SshTunnel(_host, _port, _user, _pass);
            return _ssh;
        }

        public SftpClient CreateSftpClient()
        {
            _sftp = new SftpClient(_host, _port, _user, _pass);
            return _sftp;
        }

        public void Disconnect()
        {
            if (_ssh != null)
            {
                _ssh.Disconnect();
            }

            if (_sftp != null)
            {
                _sftp.Disconnect();
            }
        }
    }

    class SshTunnel
    {
        private readonly SshClient client;

        public SshTunnel(string host, int port, string user, string pass)
        {
            client = new SshClient(host, port, user, pass);
            client.Connect();
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public string ExecuteCommand(string cmd)
        {
            var command = client.CreateCommand(cmd);
            command.Execute();
            return command.Result;
        }
    }

    class SftpClient
    {
        private readonly Renci.SshNet.SftpClient client;

        public SftpClient(string host, int port, string user, string pass)
        {
            client = new Renci.SshNet.SftpClient(host, port, user, pass);
            client.Connect();        
        }

        public void ChangeDir(string dir)
        {
            try
            {
                client.ChangeDirectory(dir);
            } catch (Renci.SshNet.Common.SftpPathNotFoundException)
            {
                throw new ArgumentException("Directory given is not valid");
            }
        }

        public List<SftpFile> List(bool isDir = false, string pattern = null)
        {
            List<SftpFile> fileList = new List<SftpFile>();
            IEnumerable<SftpFile> files = client.ListDirectory(WorkingDirectory());

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

                    if (rgx.IsMatch(file.Name) && file.IsDirectory == isDir){
                        fileList.Add(file);
                    }
                }
            }

            return fileList;
        }

        public string WorkingDirectory()
        {
            return client.WorkingDirectory;
        }

        public void Disconnect()
        {
            client.Disconnect();
        }
    }
}
