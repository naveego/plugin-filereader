using System.IO;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        /// <summary>
        /// Creates a new sql connection
        /// </summary>
        /// <param name="dbFilePrefix"></param>
        /// <returns>An open sql connection</returns>
        public static SqlDatabaseConnection GetSqlConnection(string dbFilePrefix)
        {
            Directory.CreateDirectory(Constants.DbFolder);
            
            var connBuilder = new SqlDatabaseConnectionStringBuilder
            {
                DatabaseFileMode = DatabaseFileMode.OpenOrCreate,
                DatabaseMode = DatabaseMode.ReadWrite,
                SchemaName = Constants.SchemaName,
                Uri = $"file://{Path.Join(Constants.DbFolder, $"{dbFilePrefix}_{Constants.DbFile}")}"
            };
            var conn = new SqlDatabaseConnection(connBuilder.ConnectionString);
            conn.Open();

            return conn;
        }
    }
}