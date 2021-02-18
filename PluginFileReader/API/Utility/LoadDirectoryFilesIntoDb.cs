using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PluginFileReader.API.Factory;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static void LoadDirectoryFilesIntoDb(
            IImportExportFactory factory, SqlDatabaseConnection conn, RootPathObject rootPath,
            string tableName, string schemaName, List<string> paths, long recordLimit = long.MaxValue, int fileLimit = int.MaxValue
            )
        {
            DeleteDirectoryFilesFromDb(conn, tableName, schemaName);

            foreach (var path in paths.Take(fileLimit))
            {
                ImportRecordsForPath(factory, conn, rootPath, tableName, schemaName, path,
                    recordLimit);
            }
        }
    }
}