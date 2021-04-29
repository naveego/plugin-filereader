using System;
using System.Collections.Generic;
using System.Linq;
using Naveego.Sdk.Logging;
using PluginFileReader.API.Factory;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static void DeleteDirectoryFilesFromDb(SqlDatabaseConnection conn, string tableName, string schemaName,
            IImportExportFactory factory, RootPathObject rootPath, List<string> paths)
        {
            try
            {
                var tableNames = factory.MakeImportExportFile(conn, rootPath, tableName, schemaName)
                    .GetAllTableNames();
                
                foreach (var table in tableNames)
                {
                    var tableNameId = $"[{table.SchemaName}].[{table.TableName}]";
                    Logger.Info($"Purging table: {tableNameId}");
                    var cmd = new SqlDatabaseCommand
                    {
                        Connection = conn,
                        CommandText =
                            $@"DROP TABLE IF EXISTS {tableNameId}"
                    };

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                Logger.Error(e, "Skipping delete");
            }
        }
    }
}