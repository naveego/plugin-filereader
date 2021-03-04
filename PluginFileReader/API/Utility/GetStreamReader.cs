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
            try
            {
                Logger.Info($"Getting stream for file: {filePathAndName} from {fileMode}");
            
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
                    
                        Logger.Info($"Opened FTP stream for file: {filePathAndName} from {fileMode}");
                    
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
                    
                        Logger.Info($"Opened SFTP stream for file: {filePathAndName} from {fileMode}");

                        return sftpStream;
                    case Constants.FileModeLocal:
                    default:
                        Logger.Info($"Opened Local stream for file: {filePathAndName} from {fileMode}");
                    
                        return new FileStream(filePathAndName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Could not open stream for file: {filePathAndName} from {fileMode}");
                throw;
            }
        }
    }
}