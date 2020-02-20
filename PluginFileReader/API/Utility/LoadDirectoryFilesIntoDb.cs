using System.Collections.Generic;
using PluginFileReader.API.Factory;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static void LoadDirectoryFilesIntoDb(IImportExportFactory factory, SqlDatabaseConnection conn,
            RootPathObject rootPath,
            string tableName, string schemaName, List<string> paths, long limit = long.MaxValue)
        {
            DeleteDirectoryFilesFromDb(conn, tableName, schemaName);

            foreach (var path in paths)
            {
                ImportRecordsForPath(factory, conn, rootPath, tableName, schemaName, path,
                    limit);
            }
        }
    }
}