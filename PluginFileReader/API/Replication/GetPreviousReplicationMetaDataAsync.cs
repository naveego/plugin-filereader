using System;
using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static readonly string GetMetaDataQuery = @"SELECT * FROM {0}.{1} WHERE {2} = '{3}'";

        public static async Task<ReplicationMetaData> GetPreviousReplicationMetaDataAsync(SqlDatabaseConnection conn,
            string jobId,
            ReplicationTable table)
        {
            try
            {
                ReplicationMetaData replicationMetaData = null;

                // ensure replication metadata table
                await EnsureTableAsync(conn, table);

                // check if metadata exists
                await conn.OpenAsync();

                var query = string.Format(GetMetaDataQuery,
                    Utility.Utility.GetSafeName(table.SchemaName),
                    Utility.Utility.GetSafeName(table.TableName),
                    Utility.Utility.GetSafeName(Constants.ReplicationMetaDataJobId),
                    jobId);

                var cmd = new SqlDatabaseCommand
                {
                    Connection = conn,
                    CommandText = query
                };
                
                var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    // metadata exists
                    var request = JsonConvert.DeserializeObject<PrepareWriteRequest>(
                        reader[Constants.ReplicationMetaDataRequest].ToString());
                    var shapeName = reader[Constants.ReplicationMetaDataReplicatedShapeName]
                        .ToString();
                    var shapeId = reader[Constants.ReplicationMetaDataReplicatedShapeId]
                        .ToString();
                    var timestamp = DateTime.Parse(reader[Constants.ReplicationMetaDataTimestamp]
                        .ToString());
                    
                    replicationMetaData = new ReplicationMetaData
                    {
                        Request = request,
                        ReplicatedShapeName = shapeName,
                        ReplicatedShapeId = shapeId,
                        Timestamp = timestamp
                    };
                }

                await conn.CloseAsync();

                return replicationMetaData;
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                throw;
            }
        }
    }
}