using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Renci.SshNet;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Pgd.Magento.TimerTriggers
{
    public class CheckConnections
    {
        private readonly string _cs;
        private readonly ILogger _logger;
        private readonly ConnectionInfo _sftpConnectionInfo;
        private readonly SshClient _ssh;
        private readonly HttpClient _httpClient;

        public CheckConnections(string cs, ConnectionInfo sftpConnectionInfo, SshClient ssh, IHttpClientFactory clientFactory, ILoggerFactory logFactory)
        {
            _cs = cs;
            _logger = logFactory.CreateLogger(LogCategories.CreateFunctionUserCategory("Pgd.Magento.HttpTriggers.CheckConnections"));
            _sftpConnectionInfo = sftpConnectionInfo;
            _ssh = ssh;
            _httpClient = clientFactory.CreateClient();
        }

        [FunctionName("CheckConnections")]
        public async Task Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer)
        {
            try
            {
                IEnumerable<IPAddress> localIPs = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => !IPAddress.IsLoopback(ip));

                foreach (IPAddress ip in localIPs)
                {
                    _logger.LogInformation($"Host IP: {ip}");
                }

                HttpResponseMessage response = await _httpClient.GetAsync("https://ipinfo.io/ip");
                string responseContent = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
                _logger.LogInformation($"Outbound IP: {responseContent}");

            }
            catch
            {
                _logger.LogError("Unable to determine IP address");
            }

            try
            {
                _logger.LogInformation("Pinging SSH");
                _logger.LogInformation($"Connection state open: {_ssh.IsConnected}");
                _logger.LogInformation($"Current SSH port: {_ssh.ConnectionInfo.Port}");
                _ssh.RunCommand("echo \"test\"");
            }
            catch
            {
                _logger.LogError("SSH ping was unsuccessful");
                throw;
            }

            SftpClient sftp = new(_sftpConnectionInfo);
            try
            {
                _logger.LogInformation("Pinging SFTP");
                sftp.Connect();
                sftp.Disconnect();
            }
            catch
            {
                _logger.LogError("SFTP ping was unsuccessful");
                throw;
            }

            try
            {
                _logger.LogInformation("Pinging MySQL database");
                using MySqlConnection conn = new(_cs);
                conn.Open();

                _logger.LogInformation("Connection open, executing \"SELECT 1;\"");
                using MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.ExecuteReader();

                conn.Close();
            }
            catch
            {
                _logger.LogError("MySQL ping was unsuccessful");
                throw;
            }
        }
    }
}
