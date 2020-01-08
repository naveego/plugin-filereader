using System.Data;
using PluginCSV.API.Factory;
using PluginCSV.Helper;
using Pub;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Discover
{
    public static partial class Discover
    {
        public static Schema GetSchemaForQuery(Schema schema)
        {
            var conn = Utility.Utility.GetSqlConnection();

            var cmd = new SqlDatabaseCommand
            {
                
                Connection = conn,
                CommandText = schema.Query
            };

            var reader = cmd.ExecuteReader();
            var schemaTable = reader.GetSchemaTable();

            if (schemaTable != null)
            {
                foreach (DataColumn column in schemaTable.Columns)
                {
                    var property = new Property
                    {
                        Id = column.ColumnName,
                        Name = column.ColumnName,
                        Description = column.Caption,
                        Type = GetPropertyType(column),
                        TypeAtSource = column.DataType.ToString(),
                        IsKey = false,
                        IsNullable = column.AllowDBNull,
                        IsCreateCounter = false,
                        IsUpdateCounter = false,
                        PublisherMetaJson = ""
                    };
                
                    schema.Properties.Add(property);
                }
            }

            return schema;
        }
    }
}