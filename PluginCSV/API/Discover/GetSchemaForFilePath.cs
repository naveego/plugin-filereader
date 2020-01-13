using System.IO;
using Newtonsoft.Json;
using PluginCSV.API.Factory;
using PluginCSV.API.Utility;
using PluginCSV.DataContracts;
using PluginCSV.Helper;
using Pub;

namespace PluginCSV.API.Discover
{
    public static partial class Discover
    {
        private static Schema GetSchemaForFilePath(string schemaId, string tableName, string path, int sampleSize = 5)
        {
            var schema = new Schema
            {
                Id = schemaId,
                Name = tableName,
                DataFlowDirection = Schema.Types.DataFlowDirection.ReadWrite,
                Query = $"SELECT * FROM {schemaId}",
                Properties = {},
            };

            schema = GetSchemaForQuery(schema, sampleSize);
            
            return schema;
        }
    }
}