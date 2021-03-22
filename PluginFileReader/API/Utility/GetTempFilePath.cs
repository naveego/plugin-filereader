using System.IO;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static string GetTempFilePath(string filePathAndName)
        {
            return Path.Join(TempDirectory, filePathAndName);
        }
    }
}