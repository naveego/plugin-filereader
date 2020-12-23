using PluginFileReader.API.CSV;
using PluginFileReader.API.Excel;
using PluginFileReader.API.Factory;
using PluginFileReader.API.FixedWidthColumns;
using PluginFileReader.Helper;

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
                    return new CsvImportExportFactory();
                case Constants.FixedWidthMode:
                    return new FixedWidthColumnsFactory();
                case Constants.ExcelMode:
                    return new ExcelImportExportFactory();
                default:
                    return null;
            }
        }
    }
}