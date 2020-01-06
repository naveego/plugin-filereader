using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PluginCSV.API.Factory;
using PluginCSV.API.Utility;
using PluginCSV.DataContracts;
using PluginCSV.Helper;
using Pub;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Read
{
    public static partial class Read
    {
        /// <summary>
        /// Reads records from file at the given path
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="settings"></param>
        /// <param name="schema"></param>
        /// <returns>Records from the file</returns>
        public static IEnumerable<Record> ReadRecords(IImportExportFactory factory, Settings settings, Schema schema)
        {
            var schemaPublisherMetaJson =
                JsonConvert.DeserializeObject<SchemaPublisherMetaJson>(schema.PublisherMetaJson);

            var path = schemaPublisherMetaJson.Path;
            var schemaName = Constants.SchemaName;
            var tableName = Path.GetFileNameWithoutExtension(path);

            var conn = Utility.Utility.GetSqlConnection();
            var rowsWritten =
                Utility.Utility.ImportRecordsForPath(factory, conn, settings, tableName, schemaName, path);

            var cmd = new SqlDatabaseCommand
            {
                CommandText = $"SELECT * FROM {schema.Id}"
            };

            var reader = cmd.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var recordMap = new Dictionary<string, object>();

                    foreach (var property in schema.Properties)
                    {
                        try
                        {
                            switch (property.Type)
                            {
                                case PropertyType.String:
                                    recordMap[property.Id] = reader[property.Id].ToString();
                                    break;
                                default:
                                    recordMap[property.Id] = reader[property.Id];
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"No column with property Id: {property.Id}");
                            Logger.Error(e.Message);
                            recordMap[property.Id] = "";
                        }
                    }
                    
                    var record = new Record
                    {
                        Action = Record.Types.Action.Upsert,
                        DataJson = JsonConvert.SerializeObject(recordMap)
                    };

                    yield return record;
                }
            }
        }
    }
}