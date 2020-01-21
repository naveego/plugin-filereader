using PluginCSV.API.CSV;
using PluginCSV.API.Factory;
using PluginCSV.API.FixedWidthColumns;
using PluginCSV.Helper;

namespace PluginCSV.API.Utility
{
    public static partial class Utility
    {
        /// <summary>
        /// Gets an instance of the correct IImportExportFactory
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static IImportExportFactory GetImportExportFactory(RootPathObject rootPath)
        {
            switch (rootPath.Mode)
            {
                case "Delimited":
                    return new CsvImportExportFactory();
                case "Fixed Width Columns":
                    return new FixedWidthColumnsFactory();
                default:
                    return null;
            }
        }
    }
}