using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.CSV
{
    public class CsvImportExportFactory : IImportExportFactory
    {
        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            RootPathObject rootPath, string tableName,
            string schemaName)
        {
            return new CsvImportExport(sqlDatabaseConnection, tableName, schemaName, rootPath.Delimiter);
        }

        public IDiscoverer MakeDiscoverer()
        {
            return new CSVDiscoverer();
        }
    }
}