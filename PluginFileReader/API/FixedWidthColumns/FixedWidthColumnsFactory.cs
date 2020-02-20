using PluginFileReader.API.Factory;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.FixedWidthColumns
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