using System;

namespace Magento
{
    /// <summary>
    /// Connection to the magento server
    /// </summary>
    public class Magestack
    {
        /// <summary> Instance variable <c>Ssh</c> represents the SSH connection to the server </summary>
        public SshTunnel Ssh { get; }
        /// <summary> Instance variable <c>Sftp</c> represents the SFTP connection to the server </summary>
        public SftpClient Sftp { get; }
        /// <summary> Instance variable <c>Db</c> represents the MySQL database connection </summary>
        public MagentoDb Db { get; }
        /// <value> Host name to connect to </value>
        public string Host { get; set; }
        /// <value> Username to the Magento stack </value>
        public string User { get; set; }
        /// <value> Password to <see cref="User"/> </value>
        public string Pass { get; set; }
        /// <value> Port that the server is running on </value>
        public int Port { get; set; }

        /// <summary> Instantiates a new connection the Magento stack </summary>
        public Magestack()
        {
            Host = Environment.GetEnvironmentVariable("stack_host");
            User = Environment.GetEnvironmentVariable("stack_user");
            Pass = Environment.GetEnvironmentVariable("stack_pass");
            Port = int.Parse(Environment.GetEnvironmentVariable("stack_port"));

            Ssh = new SshTunnel(Host, Port, User, Pass);
            // Open a forwarded port for DB access
            Ssh.ForwardPort("127.0.0.1",
                uint.Parse(Environment.GetEnvironmentVariable("bound_port")), 
                Environment.GetEnvironmentVariable("db_host"),
                uint.Parse(Environment.GetEnvironmentVariable("db_port")));

            Sftp = new SftpClient(Host, Port, User, Pass);
            Db = new MagentoDb("127.0.0.1",
                uint.Parse(Environment.GetEnvironmentVariable("bound_port")),
                Environment.GetEnvironmentVariable("db_user"),
                Environment.GetEnvironmentVariable("db_pass"));
        }

        /// <summary> Disconnects from the Magento server </summary>
        public void Disconnect()
        {
            if (Ssh != null)
            {
                Ssh.Disconnect();
            }

            if (Sftp != null)
            {
                Sftp.Disconnect();
            }
        }
    }
}
