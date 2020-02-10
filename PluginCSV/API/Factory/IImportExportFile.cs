using PluginCSV.Helper;

namespace PluginCSV.API.Factory
{
    public interface IImportExportFile
    {
        /// <summary>
        /// Exports to the given FilePathAndName
        /// </summary>
        /// <param name="filePathAndName"></param>
        /// <param name="appendToFile"></param>
        /// <returns>Number of rows written out</returns>
        int ExportTable(string filePathAndName, bool appendToFile = false);
        
        /// <summary>
        /// Imports the given FilePathAndName to the in memory database
        /// </summary>
        /// <param name="filePathAndName"></param>
        /// <param name="rootPath"></param>
        /// <param name="limit"></param>
        /// <returns>Number of rows written</returns>
        long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = -1);
    }
}