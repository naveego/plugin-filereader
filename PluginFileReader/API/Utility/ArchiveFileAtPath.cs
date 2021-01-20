using System;
using System.IO;
using System.Text.RegularExpressions;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility 
    {
        public static void ArchiveFileAtPath(string path, RootPathObject rootPath, Settings settings)
        {
            try
            {
                switch (rootPath.FileReadMode)
                {
                    case Constants.FileModeLocal:
                        var archiveFilePath = GetUniqueFilePath($"{Path.Join(rootPath.ArchivePath, Path.GetFileName(path))}");
                        File.Copy(path, archiveFilePath, false);
                        DeleteFileAtPath(path, rootPath, settings, false);
                        break;
                    case Constants.FileModeFtp:
                        using (var client = GetFtpClient(settings))
                        {
                            try
                            {
                                var remoteFilePath = Path.Join("/", path.Replace(TempDirectory, ""));
                                client.MoveFile(remoteFilePath, rootPath.ArchivePath);
                                DeleteFileAtPath(path, rootPath, settings, true);
                            }
                            finally
                            {
                                client.Disconnect();
                            }
                        }

                        break;
                    case Constants.FileModeSftp:
                        using (var client = GetSftpClient(settings))
                        {
                            try
                            {
                                var localFileStream = GetFileStream(path);
                                client.UploadFile(localFileStream, rootPath.ArchivePath);
                                DeleteFileAtPath(path, rootPath, settings, true);
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