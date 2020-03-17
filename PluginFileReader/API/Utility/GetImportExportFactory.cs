using PluginFileReader.API.Factory;
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
        public static IImportExportFactory GetImportExportFactory(RootPathObject rootPath)
        {
            switch (rootPath.Mode)
            {
                case "Delimited":
                    return new CsvImportExportFactory();
                case "Fixed Width Columns":
                    return new FixedWidthColumnsFactory();
                case "AS400":
                    return new AS400Factory();
                default:
                    return null;
            }
        }
    }
}