using System;
using System.IO;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static string TempDirectory = "";

        public static StreamReader GetStreamReader(string filePathAndName)
        {
            return new StreamReader(GetFileStream(filePathAndName));
        }

        public static FileStream GetFileStream(string filePathAndName)
        {
            return new FileStream(filePathAndName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}