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
                var archiveFilePath = $"{archivePath.TrimEnd('/')}/{Path.GetFileNameWithoutExtension(path)}";
                File.Copy(path, archiveFilePath, true);
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to archive file {path}");
                Logger.Error(e.Message);
            }
        }
    }
}