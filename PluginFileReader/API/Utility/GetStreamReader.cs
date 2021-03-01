using System;
using System.IO;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static string TempDirectory = "";

        public static StreamReader GetStreamReader(string filePathAndName, string fileMode)
        {
            return new StreamReader(GetStream(filePathAndName, fileMode));
        }

        public static Stream GetStream(string filePathAndName, string fileMode)
        {
            switch (fileMode)
            {
                case Constants.FileModeFtp:
                    using (var client = Utility.GetFtpClient())
                    {
                        var stream = client.OpenRead(filePathAndName);
                        client.Disconnect();
                        
                        return stream;
                    }

                    break;
                case Constants.FileModeSftp:
                    using (var client = Utility.GetSftpClient())
                    {
                        var stream = client.OpenRead(filePathAndName);
                        client.Disconnect();

                        return stream;
                    }

                    break;
                case Constants.FileModeLocal:
                default:
                    return new FileStream(filePathAndName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }
    }
}