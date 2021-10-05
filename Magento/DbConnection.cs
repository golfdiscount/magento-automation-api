using MySql.Data.MySqlClient;

namespace Magento
{
    public class MagentoDb
    {
        private MySqlConnection _conn;

        public MagentoDb(string host, uint port, string user, string pass)
        {
            MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = port,
                UserID = user,
                Password = pass,
                Database = "golfdi_mage2"
            };

            Connect(connString);
        }

        private void Connect()
        {
            _conn.Open();
        }

        private void Connect(MySqlBaseConnectionStringBuilder connString)
        {
            _conn = new MySqlConnection(connString.ConnectionString);
            _conn.Open();
        }

        public MySqlDataReader ExecuteDbCommand(string cmd)
        {
            if (!_conn.Ping())
            {
                Connect();
            }

            MySqlCommand dbCmd = _conn.CreateCommand();
            dbCmd.CommandText = cmd;

            return dbCmd.ExecuteReader();
        }

        public void Disconnect()
        {
            _conn.Dispose();
        }
    }
}