using System;
using System.Collections.Generic;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Read
{
    public static partial class Read
    {
        /// <summary>
        /// Reads records for schema
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="dbFilePrefix"></param>
        /// <returns>Records from the file</returns>
        public static IEnumerable<Record> ReadRecords(Schema schema, string dbFilePrefix)
        {
            var query = schema.Query;

            if (string.IsNullOrWhiteSpace(query))
            {
                query = Utility.Utility.GetDefaultQuery(schema);
            }
            
            var conn = Utility.Utility.GetSqlConnection(dbFilePrefix);

            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText = query
            };

            SqlDatabaseDataReader reader;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                yield break;
            }
            
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
                            Logger.Error(e, $"No column with property Id: {property.Id}");
                            Logger.Error(e, e.Message);
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