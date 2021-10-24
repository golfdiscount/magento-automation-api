using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Renci.SshNet.Sftp;

namespace Magento
{
    /// <summary> SFTP client with basic functionalities like changing directories and grabbing files </summary>
    public class SftpClient
    {
        // Maximum number of files to keep track of
        private const int MaxFileTrack = 20;
        private readonly Renci.SshNet.SftpClient client;
        private readonly Queue<SftpFile> RecentFiles = new Queue<SftpFile>(MaxFileTrack);

        /// <summary> Instantiates an SFTP client on the Magento server </summary>
        /// <param name="host">Host to connect to</param>
        /// <param name="port">Port number to connect to</param>
        /// <param name="user">SFTP username</param>
        /// <param name="pass">SFTP password</param>
        public SftpClient(string host, int port, string user, string pass)
        {
            client = new Renci.SshNet.SftpClient(host, port, user, pass)
            {
                // Send a keep alive interval every minute
                KeepAliveInterval = new TimeSpan(0, 1, 0)
            };

            Connect();
        }

        /// <summary> Connects to the SFTP server </summary>
        public void Connect()
        {
            client.Connect();
        }

        /// <summary> Change the current working directory </summary>
        /// <param name="dir">Name of directory to change to</param>
        /// <exception cref="ArgumentException">The given directory name is not valid</exception>
        public void ChangeDir(string dir)
        {
            try
            {
                client.ChangeDirectory(dir);
            }
            catch (Renci.SshNet.Common.SftpPathNotFoundException)
            {
                throw new ArgumentException("Directory given is not valid");
            }
        }

        /// <summary> List out the files in the current directory </summary>
        /// <param name="isDir">True if you want to include directories, false if not. Defaults to false.</param>
        /// <param name="pattern">Regex pattern for file and directory matching. Defaults to no Regex pattern</param>
        /// <returns>List of <c>SftpFile</c> objects found in the current working directory</returns>
        public List<SftpFile> List(bool isDir = false, string pattern = null)
        {
            List<SftpFile> fileList = new List<SftpFile>();
            IEnumerable<SftpFile> files;
            try
            {
                files = client.ListDirectory(this.WorkingDirectory);
            } catch (Renci.SshNet.Common.SshConnectionException)
            {
                client.Connect();
                files = client.ListDirectory(this.WorkingDirectory);
            }
            

            foreach (SftpFile file in files)
            {
                if (pattern is null)
                {
                    if (file.IsDirectory == isDir)
                    {
                        fileList.Add(file);
                    }
                }
                else
                {
                    Regex rgx = new Regex(pattern);

                    if (rgx.IsMatch(file.Name) && file.IsDirectory == isDir)
                    {
                        fileList.Add(file);
                    }
                }
            }

            return fileList;
        }

        /// <summary> Reads a file </summary>
        /// <param name="file">File to be read</param>
        /// <returns><c>byte[]</c> of file contents</returns>
        public byte[] ReadFile(SftpFile file)
        {
            return this.client.ReadAllBytes(file.FullName);
        }

        /// <summary>
        /// Adds a file to the file tracking queue. If the queue already has 20 files, 
        /// a file will be dequeued from the front and the new file enqueued to the back
        /// </summary>
        /// <param name="file">File to be tracked</param>
        public void TrackFile(SftpFile file)
        {
            if (RecentFiles.Count == MaxFileTrack)
            {
                RecentFiles.Dequeue();
            }

            RecentFiles.Enqueue(file);
        }

        /// <summary> Disconnects the SFTP client from the server </summary>
        public void Disconnect()
        {
            client.Disconnect();
        }

        /// <summary> Gets the current working directory or <see cref="ChangeDir(string)"/> to the given directory </summary>
        public string WorkingDirectory
        {
            get { return client.WorkingDirectory; }
            set { ChangeDir(value); }
        }
    }
}
