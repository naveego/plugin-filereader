using System.Collections.Generic;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Factory
{
    public interface IImportExportFile
    {
        /// <summary>
        /// Exports to the given FilePathAndName
        /// </summary>
        /// <param name="filePathAndName"></param>
        /// <param name="appendToFile"></param>
        /// <returns>Number of rows written out</returns>
        long ExportTable(string filePathAndName, bool appendToFile = false);
        
        /// <summary>
        /// Imports the given FilePathAndName to the in memory database
        /// </summary>
        /// <param name="filePathAndName"></param>
        /// <param name="rootPath"></param>
        /// <param name="limit"></param>
        /// <returns>Number of rows written</returns>
        long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = -1);

        /// <summary>
        /// Writes a line to a file
        /// A line number of -1 specifies to append to the end of the file
        /// </summary>
        /// <param name="filePathAndName"></param>
        /// <param name="recordMap"></param>
        /// <param name="includeHeader"></param>
        /// <param name="lineNumber"></param>
        /// <returns>the line number of record written to the file</returns>
        long WriteLineToFile(string filePathAndName, Dictionary<string, object> recordMap, bool includeHeader = false, long lineNumber = -1);

        /// <summary>
        /// Gets all table names
        /// </summary>
        /// <returns></returns>
        List<SchemaTable> GetAllTableNames(string filePathAndName);
    }

    public class SchemaTable
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
    }
}