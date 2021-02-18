using System.Net;
using FluentFTP;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static FtpClient GetFtpClient(Settings settings)
        {
            var client = new FtpClient(settings.FtpHostname);
            client.Credentials = new NetworkCredential(settings.FtpUsername, settings.FtpPassword);
            client.Port = settings.FtpPort.Value;

            client.Connect();

            return client;
        }
    }
}