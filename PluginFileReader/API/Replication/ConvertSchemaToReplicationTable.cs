using System.Collections.Generic;
using Naveego.Sdk.Plugins;
using PluginFileReader.DataContracts;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication 
    {
        public static ReplicationTable ConvertSchemaToReplicationTable(Schema schema, string schemaName,
            string tableName)
        {
            var table = new ReplicationTable
            {
                SchemaName = schemaName,
                TableName = tableName,
                Columns = new List<ReplicationColumn>()
            };
            
            foreach (var property in schema.Properties)
            {
                var column = new ReplicationColumn
                {
                    ColumnName = property.Name,
                    DataType = $"VARCHAR({int.MaxValue})",
                    PrimaryKey = false
                };
                
                table.Columns.Add(column);
            }

            return table;
        }
    }
}