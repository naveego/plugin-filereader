using System;
using System.IO;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static string TempDirectory = "";

        public static StreamReader GetStreamReader(string filePathAndName, RootPathObject rootPath)
        {
            return new StreamReader(GetFileStream(filePathAndName, rootPath));
        }

        public static FileStream GetFileStream(string filePathAndName, RootPathObject rootPath)
        {
            switch (rootPath.FileReadMode)
            {
                default:
                    return new FileStream(filePathAndName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }
    }
}