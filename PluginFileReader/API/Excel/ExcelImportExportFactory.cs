using PluginFileReader.API.Factory;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Excel
{
    public class ExcelImportExportFactory : IImportExportFactory
    {
        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection,
            RootPathObject rootPath,
            string tableName, string schemaName)
        {
            return new ExcelImportExport(sqlDatabaseConnection, rootPath, tableName, schemaName);
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
    }
}