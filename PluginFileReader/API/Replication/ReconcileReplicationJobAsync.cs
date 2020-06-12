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
        private const string SchemaNameChange = "Schema name changed";
        private const string GoldenNameChange = "Golden record name changed";
        private const string VersionNameChange = "Version name changed";
        private const string JobDataVersionChange = "Job data version changed";
        private const string ShapeDataVersionChange = "Shape data version changed";
        
        public static async Task ReconcileReplicationJobAsync(SqlDatabaseConnection conn, PrepareWriteRequest request)
        {
            // get request settings 
            var replicationSettings =
                JsonConvert.DeserializeObject<ConfigureReplicationFormData>(request.Replication.SettingsJson);
            var safeSchemaName = Constants.SchemaName;
            var safeGoldenTableName =
                replicationSettings.GetGoldenTableName();
            var safeVersionTableName =
                replicationSettings.GetVersionTableName();

            var metaDataTable = new ReplicationTable
            {
                SchemaName = safeSchemaName,
                TableName = Constants.ReplicationMetaDataTableName,
                Columns = Constants.ReplicationMetaDataColumns
            };

            var goldenTable = GetGoldenReplicationTable(request.Schema, safeSchemaName, safeGoldenTableName);
            var versionTable = GetVersionReplicationTable(request.Schema, safeSchemaName, safeVersionTableName);

            Logger.Info(
                $"SchemaName: {safeSchemaName} Golden Table: {safeGoldenTableName} Version Table: {safeVersionTableName} job: {request.DataVersions.JobId}");

            // get previous metadata
            Logger.Info($"Getting previous metadata job: {request.DataVersions.JobId}");
            var previousMetaData = await GetPreviousReplicationMetaDataAsync(conn, request.DataVersions.JobId, metaDataTable);
            Logger.Info($"Got previous metadata job: {request.DataVersions.JobId}");

            // create current metadata
            Logger.Info($"Generating current metadata job: {request.DataVersions.JobId}");
            var metaData = new ReplicationMetaData
            {
                ReplicatedShapeId = request.Schema.Id,
                ReplicatedShapeName = request.Schema.Name,
                Timestamp = DateTime.Now,
                Request = request
            };
            Logger.Info($"Generated current metadata job: {request.DataVersions.JobId}");

            // check if changes are needed
            if (previousMetaData == null)
            {
                Logger.Info($"No Previous metadata creating tables job: {request.DataVersions.JobId}");
                await EnsureTableAsync(conn, goldenTable);
                await EnsureTableAsync(conn, versionTable);
                Logger.Info($"Created tables job: {request.DataVersions.JobId}");
            }
            else
            {
                var dropGoldenReason = "";
                var dropVersionReason = "";
                var previousReplicationSettings =
                    JsonConvert.DeserializeObject<ConfigureReplicationFormData>(previousMetaData.Request.Replication
                        .SettingsJson);
                
                var previousGoldenTable = ConvertSchemaToReplicationTable(previousMetaData.Request.Schema, Constants.SchemaName, previousReplicationSettings.GetGoldenTableName());

                var previousVersionTable = ConvertSchemaToReplicationTable(previousMetaData.Request.Schema, Constants.SchemaName, previousReplicationSettings.GetVersionTableName());

                // check if schema changed
                // if (Constants.SchemaName != Constants.SchemaName)
                // {
                //     dropGoldenReason = SchemaNameChange;
                //     dropVersionReason = SchemaNameChange;
                // }

                // check if golden table name changed
                if (previousReplicationSettings.GetGoldenTableName() != replicationSettings.GetGoldenTableName())
                {
                    dropGoldenReason = GoldenNameChange;
                }

                // check if version table name changed
                if (previousReplicationSettings.GetVersionTableName() != replicationSettings.GetVersionTableName())
                {
                    dropVersionReason = VersionNameChange;
                }

                // check if job data version changed
                if (metaData.Request.DataVersions.JobDataVersion > previousMetaData.Request.DataVersions.JobDataVersion)
                {
                    dropGoldenReason = JobDataVersionChange;
                    dropVersionReason = JobDataVersionChange;
                }

                // check if shape data version changed
                if (metaData.Request.DataVersions.ShapeDataVersion >
                    previousMetaData.Request.DataVersions.ShapeDataVersion)
                {
                    dropGoldenReason = ShapeDataVersionChange;
                    dropVersionReason = ShapeDataVersionChange;
                }

                // drop previous golden table
                if (dropGoldenReason != "")
                {
                    Logger.Info($"Dropping golden table: {dropGoldenReason}");
                    await DropTableAsync(conn, previousGoldenTable);

                    await EnsureTableAsync(conn, goldenTable);
                    
                    // set triggers for async file write
                    PurgeReplicationFiles();
                    Write.Write.PurgeWriteFile();
                }

                // drop previous version table
                if (dropVersionReason != "")
                {
                    Logger.Info($"Dropping version table: {dropVersionReason}");
                    await DropTableAsync(conn, previousVersionTable);

                    await EnsureTableAsync(conn, versionTable);
                    
                    // set triggers for async file write
                    PurgeReplicationFiles();
                    Write.Write.PurgeWriteFile();
                }
            }

            // save new metadata
            Logger.Info($"Updating metadata job: {request.DataVersions.JobId}");
            await UpsertReplicationMetaDataAsync(conn, metaDataTable, metaData);
            Logger.Info($"Updated metadata job: {request.DataVersions.JobId}");
        }
    }
}