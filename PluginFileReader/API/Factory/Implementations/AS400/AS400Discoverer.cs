using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Factory.Implementations.AS400
{
    public class AS400Discoverer : IDiscoverer
    {
        public IEnumerable<Schema> DiscoverSchemas(IImportExportFactory factory, RootPathObject rootPath,
            List<string> paths, int sampleSize = 5)
        {
            var schemaName = Constants.SchemaName;
            var tableName = string.IsNullOrWhiteSpace(rootPath.Name)
                ? new DirectoryInfo(rootPath.RootPath).Name
                : rootPath.Name;
            
            var conn = Utility.Utility.GetSqlConnection(Constants.DiscoverDbPrefix);
            
            Utility.Utility.LoadDirectoryFilesIntoDb(factory, conn, rootPath, tableName, schemaName,
                paths, sampleSize, 1);

            var schemas = new List<Schema>();
            
            // foreach (var format in AS400.Format25) // POC
            foreach (var format in rootPath.ModeSettings.AS400Settings.Formats)
            {
                if (format.IsGlobalHeader)
                {
                    continue;
                }
                
                tableName = $"{tableName}_{format.KeyValue.Name}";
                var schemaId = $"[{schemaName}].[{tableName}]";
                var publisherMetaJson = new SchemaPublisherMetaJson
                {
                    RootPath = rootPath
                };
                
                var schema = new Schema
                {
                    Id = schemaId,
                    Name = tableName,
                    DataFlowDirection = Schema.Types.DataFlowDirection.ReadWrite,
                    Query = $"SELECT * FROM {schemaId}",
                    Properties = { },
                };
                
                schema = Discover.Discover.GetSchemaForQuery(schema, sampleSize, rootPath.Columns);
                schema.PublisherMetaJson = JsonConvert.SerializeObject(publisherMetaJson);
                
                schemas.Add(schema);
            }
            
            return schemas;
        }
    }
}