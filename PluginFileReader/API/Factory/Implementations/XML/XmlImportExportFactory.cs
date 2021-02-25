using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.XML
{
    public class XmlImportExportFactory : IImportExportFactory
    {
        public bool CustomDiscover { get; set; } = false;
        
        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            RootPathObject rootPath,
            string tableName, string schemaName)
        {
            return new XmlImportExport(sqlDatabaseConnection, rootPath, tableName, schemaName);
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
            throw new System.NotImplementedException();
        }
    }
}