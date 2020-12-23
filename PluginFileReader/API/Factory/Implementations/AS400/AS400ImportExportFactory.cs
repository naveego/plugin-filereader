using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.AS400
{
    public class AS400ImportExportFactory : IImportExportFactory
    {
        public bool CustomDiscover { get; set; } = true;

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            RootPathObject rootPath,
            string tableName, string schemaName)
        {
            return new AS400ImportExport(sqlDatabaseConnection, rootPath, tableName, schemaName);
        }

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            ConfigureReplicationFormData replicationFormData, string tableName, string schemaName)
        {
            throw new System.NotImplementedException();
        }

        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            ConfigureWriteFormData writeFormData, string tableName, string schemaName)
        {
            throw new System.NotImplementedException();
        }

        public IDiscoverer MakeDiscoverer()
        {
            return new AS400Discoverer();
        }
    }
}