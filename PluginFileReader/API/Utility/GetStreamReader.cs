using System;
using System.IO;
using FluentFTP;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using Renci.SshNet;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static string TempDirectory = "";
        private static FtpClient _ftpClient;
        private static SftpClient _sftpClient;

        public static StreamReader GetStreamReader(string filePathAndName, string fileMode)
        {
            return new StreamReader(GetStream(filePathAndName, fileMode));
        }

        public static Stream GetStream(string filePathAndName, string fileMode)
        {
            switch (fileMode)
            {
                case Constants.FileModeFtp:
                    if (_ftpClient == null)
                    {
                        _ftpClient = GetFtpClient();
                    }

                    if (!_ftpClient.IsConnected)
                    {
                        _ftpClient.Connect();
                    }
                    
                    var ftpStream = _ftpClient.OpenRead(filePathAndName);
                    
                    return ftpStream;
                case Constants.FileModeSftp:
                    if (_sftpClient == null)
                    {
                        _sftpClient = GetSftpClient();
                    }

                    if (!_sftpClient.IsConnected)
                    {
                        _sftpClient.Connect();
                    }

                    var sftpStream = _sftpClient.OpenRead(filePathAndName);

                    return sftpStream;
                case Constants.FileModeLocal:
                default:
                    return new FileStream(filePathAndName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }
    }
}