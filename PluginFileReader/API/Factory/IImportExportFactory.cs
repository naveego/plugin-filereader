using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory
{
    public interface IImportExportFactory
    {
        /// <summary>
        /// Indicates if the factory uses a custom discovery method
        /// </summary>
        bool CustomDiscover { get; set; } 

        /// <summary>
        /// Discover and Read Surface
        /// </summary>
        /// <param name="sqlDatabaseConnection"></param>
        /// <param name="rootPath"></param>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath, string tableName,
            string schemaName);
        
        /// <summary>
        /// Replication Surface
        /// </summary>
        /// <param name="sqlDatabaseConnection"></param>
        /// <param name="replicationFormData"></param>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, ConfigureReplicationFormData replicationFormData, string tableName,
            string schemaName);
        
        /// <summary>
        /// Write Surface
        /// </summary>
        /// <param name="sqlDatabaseConnection"></param>
        /// <param name="writeFormData"></param>
        /// <param name="tableName"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, ConfigureWriteFormData writeFormData, string tableName,
            string schemaName);
        
        /// <summary>
        /// Creates a custom discoverer
        /// </summary>
        /// <returns></returns>
        IDiscoverer MakeDiscoverer();
    }
}