using PluginFileReader.API.Factory;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.CSV
{
    public class CsvImportExportFactory: IImportExportFactory
    {
        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath, string tableName,
            string schemaName)
        {
            return new CsvImportExport(sqlDatabaseConnection, tableName, schemaName, rootPath);
        }

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            ConfigureReplicationFormData replicationFormData, string tableName, string schemaName)
        {
            return new CsvImportExport(sqlDatabaseConnection, tableName, schemaName, replicationFormData);
        }
    }
}