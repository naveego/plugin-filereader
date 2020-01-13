using System.Collections.Generic;
using PluginCSV.API.Factory;
using PluginCSV.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Utility
{
    public static partial class Utility
    {
        public static void LoadDirectoryFilesIntoDb(IImportExportFactory factory, SqlDatabaseConnection conn,
            Settings settings,
            string tableName, string schemaName, List<string> paths)
        {
            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText =
                    $@"DROP TABLE IF EXISTS [{schemaName}].[{tableName}]"
            };
            
            cmd.ExecuteNonQuery();

            foreach (var path in paths)
            {
                ImportRecordsForPath(factory, conn, settings, tableName, schemaName, path);
            }
        }
    }
}