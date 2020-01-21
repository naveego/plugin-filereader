using PluginCSV.API.Factory;
using PluginCSV.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.FixedWidthColumns
{
    public class FixedWidthColumnsFactory : IImportExportFactory
    {
        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            return new FixedWidthColumnsImportExport(sqlDatabaseConnection, rootPath, tableName, schemaName);
        }
    }
}