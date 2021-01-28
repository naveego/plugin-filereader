using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        private static readonly SemaphoreSlim WriteSemaphoreSlim = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Adds and removes records to replication db
        /// Adds and updates available shapes
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="schema"></param>
        /// <param name="record"></param>
        /// <param name="config"></param>
        /// <param name="responseStream"></param>
        /// <returns>Error message string</returns>
        public static async Task<string> WriteRecordAsync(SqlDatabaseConnection conn, Schema schema, Record record,
            ConfigureReplicationFormData config, IServerStreamWriter<RecordAck> responseStream)
        {
            // debug
            Logger.Debug($"Starting timer for {record.RecordId}");
            var timer = Stopwatch.StartNew();

            try
            {
                // debug
                Logger.Debug(JsonConvert.SerializeObject(record, Formatting.Indented));

                // semaphore
                await WriteSemaphoreSlim.WaitAsync();

                // setup
                var safeSchemaName = Constants.SchemaName;
                var safeTargetTableName = config.GetGoldenTableName();

                var targetTable =
                    Replication.Replication.GetGoldenReplicationTable(schema, safeSchemaName, safeTargetTableName);

                // get record
                var recordData = JsonConvert.DeserializeObject<Dictionary<string, object>>(record.DataJson);
                recordData[Constants.ReplicationRecordId] = record.RecordId;
                recordData[Constants.ReplicationVersionIds] = null;

                // write data
                if (recordData.Count == 2)
                {
                    // delete record
                    Logger.Debug($"shapeId: {safeSchemaName} | recordId: {record.RecordId} - DELETE");
                    await Replication.Replication.DeleteRecordAsync(conn, targetTable, record.RecordId);
                }
                else
                {
                    // add in all default values
                    foreach (var property in schema.Properties)
                    {
                        if (!recordData.ContainsKey(property.Id) && !string.IsNullOrWhiteSpace(property.PublisherMetaJson))
                        {
                            Logger.Debug("adding default value");
                            var columnConfig = JsonConvert.DeserializeObject<WriteColumn>(property.PublisherMetaJson);
                            recordData[property.Id] = columnConfig.DefaultValue;
                        }
                    }
                    
                    Logger.Debug(JsonConvert.SerializeObject(recordData, Formatting.Indented));
                    
                    // update record
                    Logger.Debug($"shapeId: {safeSchemaName} | recordId: {record.RecordId} - UPSERT");
                    await Replication.Replication.UpsertRecordAsync(conn, targetTable, recordData);
                }

                // set triggers for async file write
                LastWriteTime = DateTime.Now;
                PendingWrites = true;

                var ack = new RecordAck
                {
                    CorrelationId = record.CorrelationId,
                    Error = ""
                };
                await responseStream.WriteAsync(ack);

                timer.Stop();
                Logger.Debug($"Acknowledged Record {record.RecordId} time: {timer.ElapsedMilliseconds}");

                return "";
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Error replicating records {e.Message}");
                // send ack
                var ack = new RecordAck
                {
                    CorrelationId = record.CorrelationId,
                    Error = e.Message
                };
                await responseStream.WriteAsync(ack);

                timer.Stop();
                Logger.Debug($"Failed Record {record.RecordId} time: {timer.ElapsedMilliseconds}");
                
                if (e.Message.Contains("library routine called out of sequence"))
                {
                    throw;
                }

                return e.Message;
            }
            finally
            {
                WriteSemaphoreSlim.Release();
            }
        }
    }
}