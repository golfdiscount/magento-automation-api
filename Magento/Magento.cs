﻿using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Magento
{
    public class Magestack
    {
        private SshTunnel _ssh;
        private SftpClient _sftp;
        private MySqlConnection _mysql;
        private string _host;
        private string _user;
        private string _pass;
        private int _port = int.Parse(Environment.GetEnvironmentVariable("stack_port"));

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

        public async Task<MySqlConnection> CreateMySqlConn(string host, uint port, string user, string pass)
        {
            MySqlConnectionStringBuilder connString = new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = port,
                UserID = user,
                Password = pass,
                Database = "golfdi_mage2"
            };

            _mysql = new MySqlConnection(connString.ConnectionString);
            await _mysql.OpenAsync();
            return _mysql;
        }

        public async Task<MySqlDataReader> ExecuteMySqlCommand(string cmd)
        {
            MySqlCommand dbCmd = _mysql.CreateCommand();
            dbCmd.CommandText = cmd;

            try
            {
                return dbCmd.ExecuteReader();
            } catch (MySqlException)
            {
                // In case of connection timeout, reopen connection and execute query again
                await _mysql.OpenAsync();
                return dbCmd.ExecuteReader();
            }

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
    }
}