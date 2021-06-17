using System.Net;
using FluentFTP;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static FtpClient GetFtpClient()
        {
            var client = new FtpClient(Settings.FtpHostname);
            client.Credentials = new NetworkCredential(Settings.FtpUsername, Settings.FtpPassword);
            client.Port = Settings.FtpPort.Value;

            client.Connect();

            return client;
        }
        
        public static FtpClient GetFtpClient(FtpSettings ftpSettings)
        {
            var client = new FtpClient(ftpSettings.FtpHostname);
            client.Credentials = new NetworkCredential(ftpSettings.FtpUsername, ftpSettings.FtpPassword);
            client.Port = ftpSettings.FtpPort.Value;

            client.Connect();

            return client;
        }
    }
}