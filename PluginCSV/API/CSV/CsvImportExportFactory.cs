using PluginCSV.API.Factory;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.CSV
{
    public class CsvImportExportFactory: IImportExportFactory
    {
        public IImportExportFile MakeImportExportFile(SqlDatabaseConnection sqlDatabaseConnection, string tableName,
            string schemaName, char delimiter)
        {
            return new CsvImportExport(sqlDatabaseConnection, tableName, schemaName, delimiter);
        }

        public IImportExportFile MakeImportExportFile(string databaseFile, string tableName, string schemaName, char delimiter)
        {
            return new CsvImportExport(databaseFile, tableName, schemaName, delimiter);
        }
    }
}