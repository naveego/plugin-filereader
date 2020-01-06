using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Utility
{
    public static partial class Utility
    {
        /// <summary>
        /// Creates a new sql connection
        /// </summary>
        /// <returns>An open sql connection</returns>
        public static SqlDatabaseConnection GetSqlConnection()
        {
            var connBuilder = new SqlDatabaseConnectionStringBuilder
            {
                DatabaseFileMode = DatabaseFileMode.OpenOrCreate,
                DatabaseMode = DatabaseMode.ReadOnly,
                SchemaName = Constants.SchemaName,
                Uri = "@memory"
            };
            var conn = new SqlDatabaseConnection(connBuilder.ConnectionString);
            conn.Open();

            return conn;
        }
    }
}