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
        /// <param name="dbFolder"></param>
        /// <returns>An open sql connection</returns>
        public static SqlDatabaseConnection GetSqlConnection(string dbFilePrefix, bool onDisk = false, string dbFolder = null)
        {
            if (_connection != null)
            {
                if ((_connection.State & ConnectionState.Open) != 0)
                {
                    return _connection;
                }
            }

            if (string.IsNullOrWhiteSpace(dbFolder))
            {
                dbFolder = Constants.DbFolder;
            }
            
            Directory.CreateDirectory(dbFolder);
            
            var connBuilder = new SqlDatabaseConnectionStringBuilder
            {
                DatabaseFileMode = DatabaseFileMode.OpenOrCreate,
                DatabaseMode = DatabaseMode.ReadWrite,
                SchemaName = Constants.SchemaName,
                Uri = onDisk ? $"file://{Path.Join(dbFolder, $"{dbFilePrefix}_{Constants.DbFile}")}" : "file://@memory" 
            };
            _connection = new SqlDatabaseConnection(connBuilder.ConnectionString);
            _connection.Open();

            return _connection;
        }
    }
}