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
            return new DelimitedImportExport(sqlDatabaseConnection, tableName, schemaName, rootPath);
        }

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            ConfigureReplicationFormData replicationFormData, string tableName, string schemaName)
        {
            return new DelimitedImportExport(sqlDatabaseConnection, tableName, schemaName, replicationFormData);
        }

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            ConfigureWriteFormData writeFormData, string tableName, string schemaName)
        {
            return new DelimitedImportExport(sqlDatabaseConnection, tableName, schemaName, writeFormData);
        }

        public IDiscoverer MakeDiscoverer()
        {
            throw new System.NotImplementedException();
        }
    }
}