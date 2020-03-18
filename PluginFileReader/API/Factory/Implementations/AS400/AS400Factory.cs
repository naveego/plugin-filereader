using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.AS400
{
    public class AS400Factory : IImportExportFactory
    {
        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            RootPathObject rootPath,
            string tableName, string schemaName)
        {
            return new AS400ImportExport(sqlDatabaseConnection, rootPath, tableName, schemaName);
        }

        public IDiscoverer MakeDiscoverer()
        {
            return new AS400Discoverer();
        }
    }
}