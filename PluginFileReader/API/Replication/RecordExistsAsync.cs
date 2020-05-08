using System.Threading.Tasks;
using PluginFileReader.DataContracts;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static readonly string RecordExistsQuery = @"SELECT COUNT(*) as c
FROM (
SELECT * FROM {0}.{1}
WHERE {2} = '{3}'    
) as q";

        public static async Task<bool> RecordExistsAsync(SqlDatabaseConnection conn, ReplicationTable table,
            string primaryKeyValue)
        {
            await conn.OpenAsync();

            var query = string.Format(RecordExistsQuery,
                Utility.Utility.GetSafeName(table.SchemaName),
                Utility.Utility.GetSafeName(table.TableName),
                Utility.Utility.GetSafeName(table.Columns.Find(c => c.PrimaryKey == true).ColumnName),
                primaryKeyValue
            );
            
            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText = query
            };

            // check if record exists
            var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            var count = (long) reader["c"];
            await conn.CloseAsync();

            return count != 0;
        }
    }
}