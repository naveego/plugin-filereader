using System.Text;
using System.Threading.Tasks;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static readonly string EnsureTableQuery = @"SELECT COUNT(*) as c
FROM information_schema.tables 
WHERE table_schema = '{0}' 
AND table_name = '{1}'";
        
        // private static readonly string EnsureTableQuery = @"SELECT * FROM {0}.{1}";

        public static async Task EnsureTableAsync(SqlDatabaseConnection conn, ReplicationTable table)
        {
            // create table
            var querySb = new StringBuilder($@"CREATE TABLE IF NOT EXISTS 
{Utility.Utility.GetSafeName(table.SchemaName)}.{Utility.Utility.GetSafeName(table.TableName)}(");
            var primaryKeySb = new StringBuilder("PRIMARY KEY (");
            var hasPrimaryKey = false;
            foreach (var column in table.Columns)
            {
                querySb.Append(
                    $"{Utility.Utility.GetSafeName(column.ColumnName)} {column.DataType}{(column.PrimaryKey ? " NOT NULL UNIQUE" : "")},");
                if (column.PrimaryKey)
                {
                    primaryKeySb.Append($"{Utility.Utility.GetSafeName(column.ColumnName)},");
                    hasPrimaryKey = true;
                }
            }

            if (hasPrimaryKey)
            {
                primaryKeySb.Length--;
                primaryKeySb.Append(")");
                querySb.Append($"{primaryKeySb});");
            }
            else
            {
                querySb.Length--;
                querySb.Append(");");
            }
            
            await conn.OpenAsync();

            var query = querySb.ToString();
            Logger.Debug($"Creating Table: {query}");

            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText = query
            };

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }
    }
}