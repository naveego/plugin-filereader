using System;
using System.Collections.Generic;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using Renci.SshNet;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        private static FtpSettings Settings { get; set; }

        public static void InitializeFtpSettings(FtpSettings settings)
        {
            Settings = settings;
        }
        
        public static SftpClient GetSftpClient()
        {
            SftpClient client = null;
            
            if (!string.IsNullOrWhiteSpace(Settings.FtpPassword))
            {
                client = new SftpClient(Settings.FtpHostname, Settings.FtpPort.Value, Settings.FtpUsername, Settings.FtpPassword);
            }

            if (!string.IsNullOrWhiteSpace(Settings.FtpSshKey))
            {
                var privateKeyFiles = new []
                {
                    new PrivateKeyFile(Settings.FtpSshKey)
                };

                client = new SftpClient(Settings.FtpHostname, Settings.FtpPort.Value, Settings.FtpUsername, privateKeyFiles);
            }

            if (client != null)
            {
                client.Connect();
            }
            else
            {
                throw new Exception("SFTP Client could not be initialized.");
            }

            return client;
        }
        
        public static SftpClient GetSftpClient(FtpSettings ftpSettings)
        {
            SftpClient client = null;
            
            if (!string.IsNullOrWhiteSpace(ftpSettings.FtpPassword))
            {
                client = new SftpClient(ftpSettings.FtpHostname, ftpSettings.FtpPort.Value, ftpSettings.FtpUsername, ftpSettings.FtpPassword);
            }

            if (!string.IsNullOrWhiteSpace(ftpSettings.FtpSshKey))
            {
                var privateKeyFiles = new []
                {
                    new PrivateKeyFile(ftpSettings.FtpSshKey)
                };

                client = new SftpClient(ftpSettings.FtpHostname, ftpSettings.FtpPort.Value, ftpSettings.FtpUsername, privateKeyFiles);
            }

            if (client != null)
            {
                client.Connect();
            }
            else
            {
                throw new Exception("SFTP Client could not be initialized.");
            }

            return client;
        }
    }
}