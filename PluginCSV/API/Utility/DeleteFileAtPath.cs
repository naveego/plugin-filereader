using System;
using System.IO;
using PluginCSV.Helper;

namespace PluginCSV.API.Utility
{
    public static partial class Utility 
    {
        public static void DeleteFileAtPath(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to archive file {path}");
                Logger.Error(e.Message);
            }
        }
    }
}