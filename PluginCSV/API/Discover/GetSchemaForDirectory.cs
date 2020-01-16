using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static Schema GetSchemaForDirectory(IImportExportFactory factory, Settings settings, string directoryPath, List<string> paths,
            int sampleSize = 5)
        {
            if (paths.Count == 0)
            {
                return null;
            }
            
            var schemaName = Constants.SchemaName;
            var tableName = Directory.GetParent(paths.First()).Name;
            var schemaId = $"[{schemaName}].[{tableName}]";
            
            var conn = Utility.Utility.GetSqlConnection(Constants.DiscoverDbPrefix);
            
            Utility.Utility.LoadDirectoryFilesIntoDb(factory, conn, settings, tableName, schemaName, paths);
            
            var schemas = paths.Select(p => GetSchemaForFilePath(schemaId, tableName, p, sampleSize))
                .ToArray();

            var schema =  schemas.Last();
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson {Directory = directoryPath});
            
            return schema;
        }
    }
}