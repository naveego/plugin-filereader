using System;
using System.IO;
using PluginCSV.API.Factory;
using PluginCSV.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Utility
{
    public static partial class Utility
    {
        private static int ImportRecordsForPath(IImportExportFactory factory, SqlDatabaseConnection conn,
            Settings settings,
            string tableName, string schemaName, string path)
        {
            var importExportFile = factory.MakeImportExportFile(conn, tableName, schemaName, settings.Delimiter);
            var rowsWritten = importExportFile.ImportTable(path, settings.HasHeader);

            return rowsWritten;
        }

        private static bool ShouldImportFile(SqlDatabaseConnection conn, string path)
        {
            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText = $@"SELECT * FROM [{Constants.SchemaName}].[{Constants.ImportMetaDataTableName}]
WHERE {Constants.ImportMetaDataPathColumn} = @path",
                Parameters = { new SqlDatabaseParameter
                {
                    ParameterName = "path",
                    Value = path
                }}
            };
            
            var reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var lastModifiedMeta = DateTime.Parse(reader[Constants.ImportMetaDataLastModifiedDate].ToString());
                    var lastModifiedFile = File.GetLastWriteTimeUtc(path);

                    if (DateTime.Compare(lastModifiedMeta, lastModifiedFile) < 0)
                    {
                        // file is newer than metadata date
                        return true;
                    }

                    return false;
                }
            }

            // file not imported before
            return true;
        }

        private static void DeleteRecordsForPath()
        {
            // TODO: IF needed implement query to delete all records for a path from db
        }

        private static void CreateMetaDataTable(SqlDatabaseConnection conn)
        {
            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText =
                    $@"CREATE TABLE IF NOT EXISTS [{Constants.SchemaName}].[{Constants.ImportMetaDataTableName}] (
    {Constants.ImportMetaDataPathColumn} varchar(1024),
    {Constants.ImportMetaDataLastModifiedDate} datetime
);"
            };
            
            cmd.ExecuteNonQuery(); // Create table
        }
    }
}