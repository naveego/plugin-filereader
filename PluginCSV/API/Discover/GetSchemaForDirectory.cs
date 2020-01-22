using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginCSV.API.Factory;
using PluginCSV.API.Utility;
using PluginCSV.Helper;
using Pub;

namespace PluginCSV.API.Discover
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
            
            var conn = Utility.Utility.GetSqlConnection(Constants.DiscoverDbPrefix);
            
            Utility.Utility.LoadDirectoryFilesIntoDb(factory, conn, rootPath, tableName, schemaName, paths);
            
            var schema = new Schema
            {
                Id = schemaId,
                Name = tableName,
                DataFlowDirection = Schema.Types.DataFlowDirection.ReadWrite,
                Query = $"SELECT * FROM {schemaId}",
                Properties = {},
            };

            schema = GetSchemaForQuery(schema, sampleSize, rootPath.Columns);

            return schema;
        }
    }
}