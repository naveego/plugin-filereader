using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grpc.Core;
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
        public static IEnumerable<Schema> GetSchemasForDirectory(ServerCallContext context,
            IImportExportFactory factory, RootPathObject rootPath, List<string> paths,
            int sampleSize = 5)
        {
            if (paths.Count == 0)
            {
                return new List<Schema>();
            }

            if (sampleSize == 0)
            {
                sampleSize = 5;
            }

            if (factory.CustomDiscover)
            {
                return factory.MakeDiscoverer().DiscoverSchemas(context, factory, rootPath, paths, sampleSize);
            }

            var schemaName = Constants.SchemaName;
            var tableName = string.IsNullOrWhiteSpace(rootPath.Name)
                ? new DirectoryInfo(rootPath.RootPath).Name
                : rootPath.Name;

            var conn = Utility.Utility.GetSqlConnection(Constants.DiscoverDbPrefix);

            Utility.Utility.LoadDirectoryFilesIntoDb(factory, conn, rootPath, tableName, schemaName, paths, false, sampleSize,
                1);

            var tableNames = factory.MakeImportExportFile(conn, rootPath, tableName, schemaName)
                .GetAllTableNames(paths.FirstOrDefault());

            var schemas = new List<Schema>();

            foreach (var table in tableNames)
            {
                var tableNameId = $"[{table.SchemaName}].[{table.TableName}]";
                var schema = new Schema
                {
                    Id = tableNameId,
                    Name = table.TableName,
                    DataFlowDirection = Schema.Types.DataFlowDirection.Read,
                    Properties = { },
                };

                schema = GetSchemaForQuery(context, schema, sampleSize,
                    rootPath?.ModeSettings?.FixedWidthSettings?.Columns);

                schemas.Add(schema);
            }

            return schemas;
        }
    }
}