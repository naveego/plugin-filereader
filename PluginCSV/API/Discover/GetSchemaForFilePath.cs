using System;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using PluginCSV.API.Factory;
using PluginCSV.API.Utility;
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
            
            var conn = Utility.Utility.GetSqlConnection();
            var rowsWritten =
                Utility.Utility.ImportRecordsForPath(factory, conn, settings, tableName, schemaName, path);
            
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
                Query = $"SELECT * FROM {schemaId}",
                Properties = {},
            };

            schema = GetSchemaForQuery(schema);
            
            return schema;
        }
    }
}