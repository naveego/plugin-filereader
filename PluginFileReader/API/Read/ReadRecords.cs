using System;
using System.Collections.Generic;
using Grpc.Core;
using Naveego.Sdk.Logging;
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
        /// <param name="context"></param>
        /// <param name="schema"></param>
        /// <param name="dbFilePrefix"></param>
        /// <returns>Records from the file</returns>
        public static IEnumerable<Record> ReadRecords(ServerCallContext context, Schema schema, string dbFilePrefix)
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
                Logger.Info($"Executing query");
                Logger.Debug(query);
                
                reader = cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                Logger.Error(e,$"Failed to execute query");
                Logger.Debug(query);
                Logger.Error(e, e.Message, context);
                throw;
            }
            
            Logger.Info("Executed query successfully");
            
            if (reader.HasRows)
            {
                Logger.Info("Results set has rows. Reading...");
                
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
                            Logger.Debug($"No column with property Id: {property.Id}\n{e.Message}\n{e.StackTrace}");
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