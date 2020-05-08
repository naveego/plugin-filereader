using System.Threading.Tasks;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static readonly string DeleteRecordQuery = @"DELETE FROM {0}.{1}
WHERE {2} = '{3}'";

        public static async Task DeleteRecordAsync(SqlDatabaseConnection conn, ReplicationTable table,
            string primaryKeyValue)
        {
            await conn.OpenAsync();
            
            //TODO: first find record line number and delete it from the replication file

            var query = string.Format(DeleteRecordQuery,
                Utility.Utility.GetSafeName(table.SchemaName),
                Utility.Utility.GetSafeName(table.TableName),
                Utility.Utility.GetSafeName(table.Columns.Find(c => c.PrimaryKey == true)
                    .ColumnName),
                primaryKeyValue
            );
            
            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText = query
            };

            cmd.ExecuteNonQuery();

            // check if table exists
            await cmd.ExecuteNonQueryAsync();

            await conn.CloseAsync();
        }
    }
}