using System;
using System.IO;
using System.Text.RegularExpressions;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility 
    {
        public static void ArchiveFileAtPath(string path, string archivePath)
        {
            try
            {
                var archiveFilePath = GetUniqueFilePath($"{Path.Join(archivePath, Path.GetFileName(path))}");
                File.Copy(path, archiveFilePath, false);
                DeleteFileAtPath(path);
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