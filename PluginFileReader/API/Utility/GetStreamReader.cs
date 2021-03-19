using System;
using System.IO;
using System.Threading.Tasks;
using FluentFTP;
using PluginFileReader.Helper;
using Renci.SshNet;

namespace PluginFileReader.API.Utility
{
    public class StreamWrapper
    {
        public FtpClient FtpClient { get; set; } = null;
        public SftpClient SftpClient { get; set; } = null;
        public Stream Stream { get; set; }
        public StreamReader StreamReader
        {
            get
            {
                if (_streamReader != null)
                {
                    return _streamReader;
                }

                _streamReader = new StreamReader(Stream);
                
                return _streamReader;
            }
        }

        private StreamReader _streamReader { get; set; } = null;

        public void Close()
        {
            FtpClient?.Disconnect();
            SftpClient?.Disconnect();
            StreamReader?.Close();
            Stream?.Close();
        }
    }
    
    public static partial class Utility
    {
        public static string TempDirectory = "";
        // private static FtpClient _ftpClient;
        // private static SftpClient _sftpClient;

        // public static StreamReader GetStreamReader(string filePathAndName, string fileMode)
        // {
        //     return new StreamReader(GetStream(filePathAndName, fileMode));
        // }

        public static StreamWrapper GetStream(string filePathAndName, string fileMode)
        {
            try
            {
                Logger.Info($"Getting stream for file: {filePathAndName} from {fileMode}");
            
                switch (fileMode)
                {
                    case Constants.FileModeFtp:
                        var ftpClient = GetFtpClient();

                        if (!ftpClient.IsConnected)
                        {
                            ftpClient.Connect();
                        }
                    
                        var ftpStream = ftpClient.OpenRead(filePathAndName);
                    
                        Logger.Info($"Opened FTP stream for file: {filePathAndName} from {fileMode}");

                        return new StreamWrapper
                        {
                            FtpClient = ftpClient,
                            Stream = ftpStream
                        };
                    case Constants.FileModeSftp:
                        var sftpClient = GetSftpClient();

                        if (!sftpClient.IsConnected)
                        {
                            sftpClient.Connect();
                        }
                        
                        var sftpStream = sftpClient.OpenRead(filePathAndName);
                    
                        Logger.Info($"Opened SFTP stream for file: {filePathAndName} from {fileMode}");

                        return new StreamWrapper
                        {
                            SftpClient = sftpClient,
                            Stream = sftpStream
                        };
                    case Constants.FileModeLocal:
                    default:
                        Logger.Info($"Opened Local stream for file: {filePathAndName} from {fileMode}");
                    
                        return new StreamWrapper
                        {
                            Stream = new FileStream(filePathAndName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                        };
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