using System;

namespace Magento
{
    public class Magestack
    {
        private readonly SshTunnel _ssh;
        private readonly SftpClient _sftp;
        private readonly MagentoDb _mysql;
        private string _host;
        private string _user;
        private string _pass;
        private int _port;

        public Magestack()
        {
            _host = Environment.GetEnvironmentVariable("stack_host");
            _user = Environment.GetEnvironmentVariable("stack_user");
            _pass = Environment.GetEnvironmentVariable("stack_pass");
            _port = int.Parse(Environment.GetEnvironmentVariable("stack_port"));

            _ssh = new SshTunnel(_host, _port, _user, _pass);
            _ssh.ForwardPort("127.0.0.1", 3307, 
                Environment.GetEnvironmentVariable("db_host"),
                uint.Parse(Environment.GetEnvironmentVariable("db_port")));

            _sftp = new SftpClient(_host, _port, _user, _pass);
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

        // Getters and setters
        public string Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public string User
        {
            get { return _user; }
            set { _user = value; }
        }

        public string Pass
        {
            set { _pass = value; }
        }

        public int Port
        {
            get { return _port; }
            set { _port = value; }
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
