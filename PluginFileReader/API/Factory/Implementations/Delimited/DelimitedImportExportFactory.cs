using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.Delimited
{
    public class DelimitedImportExportFactory: IImportExportFactory
    {
        public bool CustomDiscover { get; set; } = false;

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath, string tableName,
            string schemaName)
        {
            return new DelimitedImportExport(sqlDatabaseConnection, rootPath, tableName, schemaName);
        }

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            ConfigureReplicationFormData replicationFormData, string tableName, string schemaName)
        {
            return new DelimitedImportExport(sqlDatabaseConnection, replicationFormData, tableName, schemaName);
        }

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            ConfigureWriteFormData writeFormData, string tableName, string schemaName)
        {
            return new DelimitedImportExport(sqlDatabaseConnection, writeFormData, tableName, schemaName);
        }

        public IDiscoverer MakeDiscoverer()
        {
            throw new System.NotImplementedException();
        }
    }
}