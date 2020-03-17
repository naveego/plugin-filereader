using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory
{
    public interface IImportExportFactory
    {
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath, string tableName,
            string schemaName);
        IDiscoverer MakeDiscoverer();
    }
}