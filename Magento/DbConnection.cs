using MySql.Data.MySqlClient;

namespace Magento
{
    /// <summary> Object representing access to the Magento database </summary>
    public class MagentoDb
    {
        private MySqlConnection _conn;

        /// <summary> Instatiates a new connection to the Magento database </summary>
        /// <param name="host"> Host name of the database </param>
        /// <param name="port"> Port the database is hosted on </param>
        /// <param name="user"> Username to access database</param>
        /// <param name="pass"> Password to database user </param>
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

        /// <summary> Opens a connection to the database </summary>
        private void Connect()
        {
            _conn.Open();
        }

        /// <summary> Opens a connection to the database with a connection string </summary>
        /// <param name="connString"><see cref="MySqlBaseConnectionStringBuilder"/> with connection string information </param>
        private void Connect(MySqlBaseConnectionStringBuilder connString)
        {
            _conn = new MySqlConnection(connString.ConnectionString);
            _conn.Open();
        }

        /// <summary> Executes a command on the database </summary>
        /// <param name="cmd">Command to be executed </param>
        /// <returns><see cref="MySqlDataReader"/> with results from command execution </returns>
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

        /// <summary> Disconnects from the database </summary>
        public void Disconnect()
        {
            _conn.Dispose();
        }
    }
}