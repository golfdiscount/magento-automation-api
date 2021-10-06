using System;

namespace Magento
{
    public class Magestack
    {
        private readonly SshTunnel _ssh;
        private readonly SftpClient _sftp;
        private readonly MagentoDb _mysql;
        public string Host { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public int Port { get; set; }

        public Magestack()
        {
            Host = Environment.GetEnvironmentVariable("stack_host");
            User = Environment.GetEnvironmentVariable("stack_user");
            Pass = Environment.GetEnvironmentVariable("stack_pass");
            Port = int.Parse(Environment.GetEnvironmentVariable("stack_port"));

            _ssh = new SshTunnel(Host, Port, User, Pass);
            _ssh.ForwardPort("127.0.0.1", 3307, 
                Environment.GetEnvironmentVariable("db_host"),
                uint.Parse(Environment.GetEnvironmentVariable("db_port")));

            _sftp = new SftpClient(Host, Port, User, Pass);
            _mysql = new MagentoDb("127.0.0.1",
                3307,
                Environment.GetEnvironmentVariable("db_user"),
                Environment.GetEnvironmentVariable("db_pass"));
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

        public SftpClient Sftp
        {
            get { return _sftp; }
        }

        public SshTunnel Ssh
        {
            get { return _ssh; }
        }

        public MagentoDb Database
        {
            get { return _mysql; }
        }
    }
}
