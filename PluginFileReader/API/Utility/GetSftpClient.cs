using System;
using System.Collections.Generic;
using PluginFileReader.Helper;
using Renci.SshNet;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static SftpClient GetSftpClient(Settings settings)
        {
            SftpClient client = null;
            
            if (!string.IsNullOrWhiteSpace(settings.FtpPassword))
            {
                client = new SftpClient(settings.FtpHostname, settings.FtpPort.Value, settings.FtpUsername, settings.FtpPassword);
            }

            if (!string.IsNullOrWhiteSpace(settings.FtpSshKey))
            {
                var privateKeyFiles = new []
                {
                    new PrivateKeyFile(settings.FtpSshKey)
                };

                client = new SftpClient(settings.FtpHostname, settings.FtpPort.Value, settings.FtpUsername, privateKeyFiles);
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