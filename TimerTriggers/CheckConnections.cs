using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Renci.SshNet;

namespace Pgd.Magento.TimerTriggers
{
    public class CheckConnections
    {
        private readonly string _cs;
        private readonly ILogger _logger;
        private readonly SftpClient _sftp;
        private readonly SshClient _ssh;

        public CheckConnections(string cs, SftpClient sftp, SshClient ssh, ILoggerFactory logFactory)
        {
            _cs = cs;
            _logger = logFactory.CreateLogger(LogCategories.CreateFunctionUserCategory("Pgd.Magento.HttpTriggers.CheckConnections"));
            _sftp = sftp;
            _ssh = ssh;
        }

        [FunctionName("CheckConnections")]
        public void Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, ILogger log)
        {
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

            try
            {
                _logger.LogInformation("Pinging SFTP");
                _sftp.Connect();
                _sftp.Disconnect();
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
