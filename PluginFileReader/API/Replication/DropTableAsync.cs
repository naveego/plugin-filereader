using System.Threading.Tasks;
using PluginFileReader.DataContracts;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static readonly string DropTableQuery = @"DROP TABLE IF EXISTS {0}.{1}";

        public static async Task DropTableAsync(SqlDatabaseConnection conn, ReplicationTable table)
        {
            await conn.OpenAsync();
            
            // TODO: delete file from disk

            var query = string.Format(DropTableQuery,
                Utility.Utility.GetSafeName(table.SchemaName),
                Utility.Utility.GetSafeName(table.TableName)
            );
            
            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText = query
            };

            cmd.ExecuteNonQuery();

            await conn.CloseAsync();
        }
    }
}