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
            if (paths.Count == 0)
            {
                return new List<Schema>();
            }

            if (sampleSize == 0)
            {
                sampleSize = 5;
            }
            
            var schemaName = Constants.SchemaName;
            var tableName = string.IsNullOrWhiteSpace(rootPath.Name)
                ? new DirectoryInfo(rootPath.RootPath).Name
                : rootPath.Name;
            
            var conn = Utility.Utility.GetSqlConnection(Constants.DiscoverDbPrefix);
            
            Utility.Utility.LoadDirectoryFilesIntoDb(factory, conn, rootPath, tableName, schemaName,
                paths.Take(1).ToList(), sampleSize);

            var schemas = new List<Schema>();
            
            // foreach (var format in rootPath.Formats)
            foreach (var format in AS400.Format25)
            {
                
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