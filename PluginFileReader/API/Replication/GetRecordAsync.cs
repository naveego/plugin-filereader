using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static readonly string GetRecordQuery = @"SELECT * FROM {0}.{1}
WHERE {2} = '{3}'";

        public static async Task<Dictionary<string, object>> GetRecordAsync(SqlDatabaseConnection conn,
            ReplicationTable table,
            string primaryKeyValue)
        {
            await conn.OpenAsync();

            var query = string.Format(GetRecordQuery,
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

            
            var reader = await cmd.ExecuteReaderAsync();

            Dictionary<string, object> recordMap = null;
            // check if record exists
            if (reader.HasRows)
            {
                await reader.ReadAsync();

                recordMap = new Dictionary<string, object>();

                foreach (var column in table.Columns)
                {
                    try
                    {
                        recordMap[column.ColumnName] = reader[column.ColumnName];
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"No column with column name: {column.ColumnName}");
                        Logger.Error(e, e.Message);
                        recordMap[column.ColumnName] = null;
                    }
                }
            }

            await conn.CloseAsync();

            return recordMap;
        }
    }
}