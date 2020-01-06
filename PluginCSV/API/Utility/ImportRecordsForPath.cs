using PluginCSV.API.Factory;
using PluginCSV.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Utility
{
    public static partial class Utility
    {
        public static int ImportRecordsForPath(IImportExportFactory factory, SqlDatabaseConnection conn, Settings settings,
            string tableName, string schemaName, string path)
        {
            var importExportFile = factory.MakeImportExportFile(conn, tableName, schemaName, settings.Delimiter);
            var rowsWritten = importExportFile.ImportTable(path, settings.HasHeader);

            return rowsWritten;
        }
    }
}