using System.Data;
using System.IO;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        private static SqlDatabaseConnection _connection;
        
        /// <summary>
        /// Creates a new sql connection
        /// </summary>
        /// <param name="dbFilePrefix"></param>
        /// <param name="onDisk"></param>
        /// <returns>An open sql connection</returns>
        public static SqlDatabaseConnection GetSqlConnection(string dbFilePrefix, bool onDisk = false)
        {
            if (_connection != null)
            {
                if ((_connection.State & ConnectionState.Open) != 0)
                {
                    return _connection;
                }
            }
            
            Directory.CreateDirectory(Constants.DbFolder);
            
            var connBuilder = new SqlDatabaseConnectionStringBuilder
            {
                DatabaseFileMode = DatabaseFileMode.OpenOrCreate,
                DatabaseMode = DatabaseMode.ReadWrite,
                SchemaName = Constants.SchemaName,
                Uri = onDisk ? $"file://{Path.Join(Constants.DbFolder, $"{dbFilePrefix}_{Constants.DbFile}")}" : "file://@memory" 
            };
            _connection = new SqlDatabaseConnection(connBuilder.ConnectionString);
            _connection.Open();

            return _connection;
        }
    }
}