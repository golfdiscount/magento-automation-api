using Renci.SshNet;
using System;

namespace Magento
{
    /// <summary> Object representing SSH access to Magento server </summary>
    public class SshTunnel
    {
        private readonly SshClient client;

        /// <summary> Inititates a new SSH session </summary>
        /// <param name="host"> Host name of SSH server </param>
        /// <param name="port"> Port SSH server is running on</param>
        /// <param name="user"> User to log onto SSH server with </param>
        /// <param name="pass"> Password to SSH user </param>
        public SshTunnel(string host, int port, string user, string pass)
        {
            client = new SshClient(host, port, user, pass)
            {   
                // Send a keep alive signal every minute
                KeepAliveInterval = new TimeSpan(0, 1, 0)
            };

            client.Connect();
        }

        /// <summary> Disconnects from the SSH server </summary>
        public void Disconnect()
        {
            foreach(ForwardedPortLocal port in client.ForwardedPorts)
            {
                port.Stop();
            }
            client.Disconnect();
        }

        /// <summary> Executes a command on the SSH server </summary>
        /// <param name="cmd"> SSH command to be executed </param>
        /// <returns> A <see cref="String"/> with results of the command execution </returns>
        public string ExecuteCommand(string cmd)
        {
            var command = client.CreateCommand(cmd);
            command.Execute();
            return command.Result;
        }

        /// <summary> Forwards a port on local machine to remote machine </summary>
        /// <param name="boundHost"> Host name on local machine to bound connection to </param>
        /// <param name="boundPort"> Port on local machine to bound connmection to </param>
        /// <param name="remoteHost"> Host on SSH server to connect to </param>
        /// <param name="remotePort"> Port on SSH server to connect to </param>
        public void ForwardPort(string boundHost, uint boundPort, string remoteHost, uint remotePort)
        {
            // Create a new connection to db1.i@stack1.c301.sonassihosting.com:3306
            // bounded to 127.0.0.1:3307
            ForwardedPortLocal portForwarded = new ForwardedPortLocal(boundHost, boundPort, remoteHost, remotePort);
            client.AddForwardedPort(portForwarded);
            portForwarded.Start();
        }

        // Getters and setters
        /// <summary> The current working directory of the SSH session </summary>
        public string WorkingDir
        {
            get { return ExecuteCommand("pwd"); }
            set { ExecuteCommand(value); }
        }
    }
}