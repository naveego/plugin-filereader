using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Factory;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Discover
{
    public static partial class Discover
    {
        public static Schema GetSchemaForDirectory(IImportExportFactory factory, RootPathObject rootPath, List<string> paths,
            int sampleSize = 5)
        {
            if (paths.Count == 0)
            {
                return null;
            }
            
            var schemaName = Constants.SchemaName;
            var tableName = new DirectoryInfo(rootPath.RootPath).Name;
            var schemaId = $"[{schemaName}].[{tableName}]";
            var publisherMetaJson = new SchemaPublisherMetaJson
            {
                RootPath = rootPath
            };
            
            var conn = Utility.Utility.GetSqlConnection(Constants.DiscoverDbPrefix);

            if (sampleSize == 0)
            {
                sampleSize = 5;
            }
            
            // Utility.Utility.LoadDirectoryFilesIntoDb(factory, conn, rootPath, tableName, schemaName, paths);
            Utility.Utility.LoadDirectoryFilesIntoDb(factory, conn, rootPath, tableName, schemaName, paths.Take(1).ToList(), sampleSize);
            
            var schema = new Schema
            {
                Id = schemaId,
                Name = tableName,
                DataFlowDirection = Schema.Types.DataFlowDirection.ReadWrite,
                Query = $"SELECT * FROM {schemaId}",
                Properties = {},
            };

            schema = GetSchemaForQuery(schema, sampleSize, rootPath.Columns);
            schema.PublisherMetaJson = JsonConvert.SerializeObject(publisherMetaJson);

            return schema;
        }
    }
}