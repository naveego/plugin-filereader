using PluginFileReader.API.Factory.Implementations.AS400;
using PluginFileReader.API.Factory.Implementations.CSV;
using PluginFileReader.API.Factory.Implementations.FixedWidthColumns;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        /// <summary>
        /// Gets an instance of the correct IImportExportFactory
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static bool GetIsMultipleSchemasForDirectory(RootPathObject rootPath)
        {
            switch (rootPath.Mode)
            {
                case "Delimited":
                    return false;
                case "Fixed Width Columns":
                    return false;
                case "AS400":
                    return true;
                default:
                    return false;
            }
        }
    }
}