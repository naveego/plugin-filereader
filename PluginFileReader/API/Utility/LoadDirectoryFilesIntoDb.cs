using System.Collections.Generic;
using System.Linq;
using Naveego.Sdk.Logging;
using Newtonsoft.Json;
using PluginFileReader.API.Factory;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static void LoadDirectoryFilesIntoDb(
            IImportExportFactory factory, SqlDatabaseConnection conn, RootPathObject rootPath,
            string tableName, string schemaName, List<string> paths, bool downloadToLocal, long recordLimit = long.MaxValue, int fileLimit = int.MaxValue,
            bool deleteAllOnStart = true)
        {
            Logger.Info($"Loading files:\n {JsonConvert.SerializeObject(paths, Formatting.Indented)}");
            
            if (deleteAllOnStart)
                DeleteDirectoryFilesFromDb(conn, tableName, schemaName, factory, rootPath, paths);

            foreach (var path in paths.Take(fileLimit))
            {
                ImportRecordsForPath(factory, conn, rootPath, tableName, schemaName, path, downloadToLocal,
                    recordLimit);
            }

            if (paths.Count > 0)
            {
                Logger.Info($"Adding indexes for {JsonConvert.SerializeObject(paths, Formatting.Indented)}");
                var importExportFile = factory.MakeImportExportFile(conn, rootPath, tableName, schemaName);
                AddIndexesToTables(importExportFile, conn);
                Logger.Info($"Added indexes for {JsonConvert.SerializeObject(paths, Formatting.Indented)}");
            }
        }
    }
}