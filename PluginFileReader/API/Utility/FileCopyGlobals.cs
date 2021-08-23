using System;

namespace PluginFileReader.API.Utility
{
    public static class FileCopyGlobals
    { 
        public static DateTime LastWriteTime { get; set; } = DateTime.Now;
    }
}