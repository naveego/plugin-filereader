using PluginFileReader.Helper;
using Renci.SshNet;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static SftpClient GetSftpClient(Settings settings)
        {
            var client = new SftpClient(settings.FtpHostname, settings.FtpPort, settings.FtpUsername, settings.FtpPassword);
            
            client.Connect();

            return client;
        }
    }
}