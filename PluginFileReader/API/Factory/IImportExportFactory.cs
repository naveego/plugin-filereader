using PluginCSV.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Factory
{
    public interface IImportExportFactory
    {
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath, string tableName,
            string schemaName);
    }
}