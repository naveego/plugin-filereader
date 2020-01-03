using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Factory
{
    public interface IImportExportFactory
    {
        IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, string tableName,
            string schemaName, char delimiter);
        IImportExportFile MakeImportExportFile(string databaseFile, string tableName, string schemaName, char delimiter);
    }
}