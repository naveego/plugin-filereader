using PluginFileReader.API.Factory;
using PluginFileReader.API.Factory.Implementations.AS400;
using PluginFileReader.API.Factory.Implementations.Delimited;
using PluginFileReader.API.Factory.Implementations.Excel;
using PluginFileReader.API.Factory.Implementations.FixedWidthColumns;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        /// <summary>
        /// Gets an instance of the correct IImportExportFactory
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static IImportExportFactory GetImportExportFactory(string mode)
        {
            switch (mode)
            {
                case Constants.DelimitedMode:
                    return new DelimitedImportExportFactory();
                case Constants.FixedWidthMode:
                    return new FixedWidthColumnsFactory();
                case Constants.ExcelMode:
                    return new ExcelImportExportFactory();
                case Constants.AS400Mode:
                    return new AS400ImportExportFactory();
                default:
                    return null;
            }
        }
    }
}