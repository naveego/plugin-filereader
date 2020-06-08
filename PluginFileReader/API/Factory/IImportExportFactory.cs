using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory
{
    public interface IImportExportFactory
    {
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath, string tableName,
            string schemaName);
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, ConfigureReplicationFormData replicationFormData, string tableName,
            string schemaName);
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, ConfigureWriteFormData writeFormData, string tableName,
            string schemaName);
    }
}