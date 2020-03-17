using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.FixedWidthColumns
{
    public class FixedWidthColumnsFactory : IImportExportFactory
    {
        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            return new FixedWidthColumnsImportExport(sqlDatabaseConnection, rootPath, tableName, schemaName);
        }

        public IDiscoverer MakeDiscoverer()
        {
            return new FixedWidthColumnsDiscoverer();
        }
    }
}