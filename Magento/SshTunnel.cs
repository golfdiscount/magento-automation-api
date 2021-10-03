using Renci.SshNet;
using System;

namespace Magento
{
    public class SshTunnel
    {
        private readonly SshClient client;

        public SshTunnel(string host, int port, string user, string pass)
        {
            client = new SshClient(host, port, user, pass);
            client.Connect();
            // Send a keep alive signal every 2 minutes
            client.KeepAliveInterval = new TimeSpan(0, 2, 0);
        }

        public void Disconnect()
        {
            foreach(ForwardedPortLocal port in client.ForwardedPorts)
            {
                port.Stop();
            }
            client.Disconnect();
        }

        public string ExecuteCommand(string cmd)
        {
            var command = client.CreateCommand(cmd);
            command.Execute();
            return command.Result;
        }

        public void ForwardPort(string boundHost, uint boundPort, string remoteHost, uint remotePort)
        {
            // Create a new connection to db1.i@stack1.c301.sonassihosting.com:3306
            // bounded to 127.0.0.1:3307
            ForwardedPortLocal portForwarded = new ForwardedPortLocal(boundHost, boundPort, remoteHost, remotePort);
            client.AddForwardedPort(portForwarded);
            portForwarded.Start();
        }

        // Getters and setters
        public string WorkingDir
        {
            get { return ExecuteCommand("pwd"); }
            set { ExecuteCommand(value); }
        }
    }
}