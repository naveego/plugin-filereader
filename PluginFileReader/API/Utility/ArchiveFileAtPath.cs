using System;
using System.IO;
using System.Text.RegularExpressions;
using Naveego.Sdk.Logging;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility 
    {
        public static void ArchiveFileAtPath(string path, RootPathObject rootPath)
        {
            try
            {
                var archiveFileName = Path.Join(rootPath.ArchivePath, Path.GetFileName(path));
                switch (rootPath.FileReadMode)
                {
                    case Constants.FileModeLocal:
                        var archiveFilePath = GetUniqueFilePath(archiveFileName);
                        File.Copy(path, archiveFilePath, false);
                        DeleteFileAtPath(path, rootPath,  false);
                        break;
                    case Constants.FileModeFtp:
                        using (var client = GetFtpClient())
                        {
                            try
                            {
                                // var localFileStream = GetStream(path, rootPath.FileReadMode, true);
                                // client.Upload(localFileStream.Stream, archiveFileName);
                                // localFileStream.Close();
                                client.MoveFile(path, archiveFileName);
                                DeleteFileAtPath(path, rootPath,  true);
                            }
                            finally
                            {
                                client.Disconnect();
                            }
                        }

                        break;
                    case Constants.FileModeSftp:
                        using (var client = GetSftpClient())
                        {
                            try
                            {
                                // var localFileStream = GetStream(path, rootPath.FileReadMode, true);
                                // client.UploadFile(localFileStream.Stream, archiveFileName);
                                // localFileStream.Close();
                                var file = client.Get(path);
                                file.MoveTo(archiveFileName);
                                DeleteFileAtPath(path, rootPath,  true);
                            }
                            finally
                            {
                                client.Disconnect();
                            }
                        }

                        break;
                }

            }
            catch (Exception e)
            {
                Logger.Error(e, $"Unable to archive file {path}");
                Logger.Error(e, e.Message);
            }
        }
        
        private static string GetUniqueFilePath(string filePath)
        {
            if (File.Exists(filePath))
            {
                string folderPath = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string fileExtension = Path.GetExtension(filePath);
                int number = 0;

                Match regex = Regex.Match(fileName, @"^(.+) \((\d+)\)$");

                if (regex.Success)
                {
                    fileName = regex.Groups[1].Value;
                    number = int.Parse(regex.Groups[2].Value);
                }

                do
                {
                    number++;
                    string newFileName = $"{fileName} ({number}){fileExtension}";
                    filePath = Path.Combine(folderPath, newFileName);
                }
                while (File.Exists(filePath));
            }

            return filePath;
        }
    }
}