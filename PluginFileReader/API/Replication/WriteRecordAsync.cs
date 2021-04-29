using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Factory;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static readonly SemaphoreSlim ReplicationSemaphoreSlim = new SemaphoreSlim(1, 1);

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
                await ReplicationSemaphoreSlim.WaitAsync();

                // setup
                var safeSchemaName = Constants.SchemaName;
                var safeGoldenTableName = config.GetGoldenTableName();
                var safeVersionTableName = config.GetVersionTableName();

                var goldenTable = GetGoldenReplicationTable(schema, safeSchemaName, safeGoldenTableName);
                var versionTable = GetVersionReplicationTable(schema, safeSchemaName, safeVersionTableName);

                // transform data
                var recordVersionIds = record.Versions.Select(v => v.RecordId).ToList();
                var recordData = GetNamedRecordData(schema, record.DataJson);
                recordData[Constants.ReplicationRecordId] = record.RecordId;
                recordData[Constants.ReplicationVersionIds] = recordVersionIds;

                // get previous golden record
                List<string> previousRecordVersionIds;
                if (await RecordExistsAsync(conn, goldenTable, record.RecordId))
                {
                    var recordMap = await GetRecordAsync(conn, goldenTable, record.RecordId);

                    if (recordMap.ContainsKey(Constants.ReplicationVersionIds))
                    {
                        previousRecordVersionIds =
                            JsonConvert.DeserializeObject<List<string>>(recordMap[Constants.ReplicationVersionIds]
                                .ToString());
                    }
                    else
                    {
                        previousRecordVersionIds = recordVersionIds;
                    }
                }
                else
                {
                    previousRecordVersionIds = recordVersionIds;
                }

                // write data
                // check if 2 since we always add 2 things to the dictionary
                if (recordData.Count == 2)
                {
                    // delete everything for this record
                    Logger.Debug($"shapeId: {safeSchemaName} | recordId: {record.RecordId} - DELETE");
                    await DeleteRecordAsync(conn, goldenTable, record.RecordId);

                    foreach (var versionId in previousRecordVersionIds)
                    {
                        Logger.Debug(
                            $"shapeId: {safeSchemaName} | recordId: {record.RecordId} | versionId: {versionId} - DELETE");
                        await DeleteRecordAsync(conn, versionTable, versionId);
                    }
                }
                else
                {
                    // update record and remove/add versions
                    Logger.Debug($"shapeId: {safeSchemaName} | recordId: {record.RecordId} - UPSERT");
                    await UpsertRecordAsync(conn, goldenTable, recordData);

                    // delete missing versions
                    var missingVersions = previousRecordVersionIds.Except(recordVersionIds);
                    foreach (var versionId in missingVersions)
                    {
                        Logger.Debug(
                            $"shapeId: {safeSchemaName} | recordId: {record.RecordId} | versionId: {versionId} - DELETE");
                        await DeleteRecordAsync(conn, versionTable, versionId);
                    }

                    // upsert other versions
                    foreach (var version in record.Versions)
                    {
                        Logger.Debug(
                            $"shapeId: {safeSchemaName} | recordId: {record.RecordId} | versionId: {version.RecordId} - UPSERT");
                        var versionData = GetNamedRecordData(schema, version.DataJson);
                        versionData[Constants.ReplicationVersionRecordId] = version.RecordId;
                        versionData[Constants.ReplicationRecordId] = record.RecordId;
                        await UpsertRecordAsync(conn, versionTable, versionData);
                    }
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
                ReplicationSemaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Converts data object with ids to friendly names
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="dataJson"></param>
        /// <returns>Data object with friendly name keys</returns>
        private static Dictionary<string, object> GetNamedRecordData(Schema schema, string dataJson)
        {
            var namedData = new Dictionary<string, object>();
            var recordData = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataJson);

            foreach (var property in schema.Properties)
            {
                var key = property.Id;
                if (!recordData.ContainsKey(key))
                {
                    continue;
                }

                namedData.Add(property.Name, recordData[key]);
            }

            return namedData;
        }
    }
}