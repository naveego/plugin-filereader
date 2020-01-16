using System;
using System.IO;
using PluginCSV.Helper;

namespace PluginCSV.API.Utility
{
    public static partial class Utility 
    {
        public static void ArchiveFileAtPath(string path, string archivePath)
        {
            try
            {
                var archiveFilePath = $"{Path.Join(archivePath, Path.GetFileName(path))}";
                File.Copy(path, archiveFilePath, true);
                DeleteFileAtPath(path);
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to archive file {path}");
                Logger.Error(e.Message);
            }
        }
    }
}