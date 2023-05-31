using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentFTP;
using Naveego.Sdk.Logging;
using PluginFileReader.Helper;
using Renci.SshNet;

namespace PluginFileReader.API.Utility
{
    public class StreamWrapper
    {
        public string TempFilePath { get; set; } = null;
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
            try
            {
                StreamReader.Close();
            }
            catch 
            {
            }
            Stream?.Close();

            if (!string.IsNullOrWhiteSpace(TempFilePath))
            {
                try
                {
                    File.Delete(TempFilePath);
                }
                catch (Exception e)
                {
                   Logger.Error(e, e.StackTrace);
                }
            }
        }

        public string PrintStreamLength()
        {
            var result = "";

            // calculate file size
            var totalBytes = this.Stream.Length;
            var suffixes = new List<string>
            {
                "B", "KB", "MB", "GB", "TB", "PB", "EB"
            };
            
            if (totalBytes == 0)
            {
                result = "0 B";
            }
            else
            {
                var bytes = Math.Abs(totalBytes);
                var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                var num = Math.Round(bytes / Math.Pow(1024, place), 1);
                result = $"{Math.Sign(totalBytes) * num}{suffixes[place]}";
            }

            return result;
        }
    }
    
    public static partial class Utility
    {
        public static string TempDirectory = "";

        public static StreamWrapper GetStream(string filePathAndName, string fileMode, bool downloadToLocal)
        {
            try
            {
                Logger.Debug($"Getting stream for file: {filePathAndName} from {fileMode}");
            
                switch (fileMode)
                {
                    case Constants.FileModeFtp:
                        var ftpClient = GetFtpClient();

                        if (!ftpClient.IsConnected)
                        {
                            ftpClient.Connect();
                        }

                        if (downloadToLocal)
                        {
                            var tempFile = GetTempFilePath(filePathAndName);
                            var tempDirectory = Path.GetDirectoryName(tempFile);
                            Directory.CreateDirectory(tempDirectory);

                            ftpClient.DownloadFile(filePathAndName, tempFile);
                            
                            return new StreamWrapper
                            {
                                FtpClient = ftpClient,
                                TempFilePath = tempFile,
                                Stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                            };
                        }
                        else
                        {
                            var ftpStream = ftpClient.OpenRead(filePathAndName);
                    
                            Logger.Debug($"Opened FTP stream for file: {filePathAndName} from {fileMode}");

                            return new StreamWrapper
                            {
                                FtpClient = ftpClient,
                                Stream = ftpStream
                            };
                        }

                    case Constants.FileModeSftp:
                        var sftpClient = GetSftpClient();

                        if (!sftpClient.IsConnected)
                        {
                            sftpClient.Connect();
                        }

                        if (downloadToLocal)
                        {
                            var tempFile = GetTempFilePath(filePathAndName);
                            var tempDirectory = Path.GetDirectoryName(tempFile);
                            Directory.CreateDirectory(tempDirectory);

                            using (var stream = File.Create(tempFile))
                            {
                                sftpClient.DownloadFile(filePathAndName, stream);
                            }

                            return new StreamWrapper
                            {
                                SftpClient = sftpClient,
                                TempFilePath = tempFile,
                                Stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                            };
                        }
                        else
                        {
                            var sftpStream = sftpClient.OpenRead(filePathAndName);
                    
                            Logger.Debug($"Opened SFTP stream for file: {filePathAndName} from {fileMode}");

                            return new StreamWrapper
                            {
                                SftpClient = sftpClient,
                                Stream = sftpStream
                            };
                        }
                    case Constants.FileModeLocal:
                    default:
                        Logger.Debug($"Opened Local stream for file: {filePathAndName} from {fileMode}");
                    
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