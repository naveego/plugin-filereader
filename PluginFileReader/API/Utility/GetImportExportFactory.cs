using PluginFileReader.API.Factory;
using PluginFileReader.API.Factory.Implementations.AS400;
using PluginFileReader.API.Factory.Implementations.Delimited;
using PluginFileReader.API.Factory.Implementations.Excel;
using PluginFileReader.API.Factory.Implementations.XML;
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
                case Constants.ModeDelimited:
                    return new DelimitedImportExportFactory();
                case Constants.ModeFixedWidth:
                    return new FixedWidthColumnsFactory();
                case Constants.ModeExcel:
                    return new ExcelImportExportFactory();
                case Constants.ModeAS400:
                    return new AS400ImportExportFactory();
                case Constants.ModeXML:
                    return new XmlImportExportFactory();
                default:
                    return null;
            }
        }
    }
}