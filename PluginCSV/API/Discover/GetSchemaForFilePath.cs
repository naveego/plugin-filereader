using System;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using PluginCSV.API.Factory;
using PluginCSV.DataContracts;
using PluginCSV.Helper;
using Pub;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Discover
{
    public static partial class Discover
    {
        public static Schema GetSchemaForFilePath(IImportExportFactory factory, Settings settings, string path)
        {
            var schemaName = Constants.SchemaName;
            var tableName = Path.GetFileNameWithoutExtension(path);
            var schemaId = $"[{schemaName}].[{tableName}]";

            var connBuilder = new SqlDatabaseConnectionStringBuilder
            {
                DatabaseFileMode = DatabaseFileMode.OpenOrCreate,
                DatabaseMode = DatabaseMode.ReadOnly,
                SchemaName = Constants.SchemaName,
                Uri = "@memory"
            };
            var conn = new SqlDatabaseConnection(connBuilder.ConnectionString);
            conn.Open();

            var importExportFile = factory.MakeImportExportFile(conn, tableName, schemaName, settings.Delimiter);

            var rowsWritten = importExportFile.ImportTable(path, settings.HasHeader);
            
            var schema = new Schema
            {
                Id = schemaId,
                Name = tableName,
                Count = new Count
                {
                    Kind = Count.Types.Kind.Exact,
                    Value = rowsWritten
                },
                DataFlowDirection = Schema.Types.DataFlowDirection.ReadWrite,
                PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson{Path = path}),
                Properties = {},
            };
            
            var schemaTable = conn.GetSchema("Columns", new string[]
            {
                $"[{schemaName}].[{tableName}]"
            });

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

            return schema;
        }
        
        private static PropertyType GetPropertyType(DataColumn column)
        {
            var type = column.DataType;
            switch (true)
            {
                case bool _ when type == typeof(bool):
                    return PropertyType.Bool;
                case bool _ when type == typeof(int):
                case bool _ when type == typeof(long):
                    return PropertyType.Integer;
                case bool _ when type == typeof(float):
                case bool _ when type == typeof(double):
                    return PropertyType.Float;
                case bool _ when type == typeof(DateTime):
                    return PropertyType.Datetime;
                case bool _ when type == typeof(string):
                    if (column.MaxLength > 1024)
                    {
                        return PropertyType.Text;
                    }
                    return PropertyType.String;
                default:
                    return PropertyType.String;
            }
        }
    }
}