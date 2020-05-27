using System;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static void DeleteDirectoryFilesFromDb(SqlDatabaseConnection conn, string tableName, string schemaName)
        {
            try
            {
                Logger.Info($"Purging table: [{schemaName}].[{tableName}]");
                var cmd = new SqlDatabaseCommand
                {
                    Connection = conn,
                    CommandText =
                        $@"DROP TABLE IF EXISTS [{schemaName}].[{tableName}]"
                };
            
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                Logger.Error(e, "Skipping delete");
            }
        }
    }
}