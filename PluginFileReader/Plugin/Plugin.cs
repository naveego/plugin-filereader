using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Discover;
using PluginFileReader.API.Read;
using PluginFileReader.API.Replication;
using PluginFileReader.API.Utility;
using PluginFileReader.API.Write;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.Plugin
{
    public class Plugin : Publisher.PublisherBase
    {
        private readonly ServerStatus _server;

        private TaskCompletionSource<bool> _tcs;
        
        private static readonly SemaphoreSlim DiscoverSemaphoreSlim = new SemaphoreSlim(1, 1);

        public Plugin()
        {
            _server = new ServerStatus();
        }

        /// <summary>
        /// Configures the plugin
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ConfigureResponse> Configure(ConfigureRequest request, ServerCallContext context)
        {
            Logger.Debug("Got configure request");
            Logger.Debug(JsonConvert.SerializeObject(request, Formatting.Indented));

            // ensure all directories are created
            Directory.CreateDirectory(request.TemporaryDirectory);
            Directory.CreateDirectory(request.PermanentDirectory);
            Directory.CreateDirectory(request.LogDirectory);

            // configure stream reader factory
            Utility.TempDirectory = request.TemporaryDirectory;

            // configure logger
            Logger.SetLogLevel(request.LogLevel);
            Logger.Init(request.LogDirectory);

            _server.Config = request;

            return Task.FromResult(new ConfigureResponse());
        }

        /// <summary>
        /// Establishes a connection with an odbc data source
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>A message indicating connection success</returns>
        public override Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
        {
            _server.Connected = false;

            // Logger.SetLogLevel(Logger.LogLevel.Debug);
            Logger.Info("Connecting...");

            // validate settings passed in
            try
            {
                _server.Settings = JsonConvert.DeserializeObject<Settings>(request.SettingsJson);
                _server.Settings.ConvertLegacySettings();
                _server.Settings.ReconcileColumnsConfigurationFiles();
                _server.Settings.ReconcileAS400FormatsFiles();
                _server.Settings.InitializeFtpSettings();
                _server.Settings.Validate();
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
                return Task.FromResult(new ConnectResponse
                {
                    OauthStateJson = "",
                    ConnectionError = "",
                    OauthError = "",
                    SettingsError = e.Message
                });
            }

            _server.Connected = true;

            Logger.Info("Connected.");

            return Task.FromResult(new ConnectResponse
            {
                OauthStateJson = "",
                ConnectionError = "",
                OauthError = "",
                SettingsError = ""
            });
        }

        /// <summary>
        /// Connects the session by forwarding requests to Connect
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ConnectSession(ConnectRequest request,
            IServerStreamWriter<ConnectResponse> responseStream, ServerCallContext context)
        {
            Logger.Info("Connecting session...");

            // create task to wait for disconnect to be called
            _tcs?.SetResult(true);
            _tcs = new TaskCompletionSource<bool>();

            // call connect method
            var response = await Connect(request, context);

            await responseStream.WriteAsync(response);

            Logger.Info("Session connected.");

            // wait for disconnect to be called
            await _tcs.Task;
        }


        /// <summary>
        /// Discovers schemas based on a query
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>Discovered schemas</returns>
        public override async Task<DiscoverSchemasResponse> DiscoverSchemas(DiscoverSchemasRequest request,
            ServerCallContext context)
        {
            Logger.SetLogPrefix("discover");
            Logger.Info("Discovering Schemas...");

            var sampleSize = checked((int) request.SampleSize);

            DiscoverSchemasResponse discoverSchemasResponse = new DiscoverSchemasResponse();

            // only return requested schemas if refresh mode selected
            if (request.Mode == DiscoverSchemasRequest.Types.Mode.All)
            {
                // get all schemas
                try
                {
                    await DiscoverSemaphoreSlim.WaitAsync();

                    var files = _server.Settings.GetAllFilesByRootPath(1);
                    Logger.Info($"Files attempted: {files.Count}");

                    var schemas = _server.Settings.RootPaths.Select(p =>
                            Discover.GetSchemasForDirectory(Utility.GetImportExportFactory(p.Mode), p,
                                files[p.RootPathName()],
                                sampleSize))
                        .ToList();

                    discoverSchemasResponse.Schemas.AddRange(schemas.SelectMany(s => s));

                    Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");

                    return discoverSchemasResponse;
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message, context);
                    return new DiscoverSchemasResponse();
                }
                finally
                {
                    DiscoverSemaphoreSlim.Release();
                }
            }

            try
            {
                await DiscoverSemaphoreSlim.WaitAsync();

                var refreshSchemas = request.ToRefresh;

                Logger.Info($"Refresh schemas attempted: {refreshSchemas.Count}");

                var files = _server.Settings.GetAllFilesByRootPath(1);
                var conn = Utility.GetSqlConnection(Constants.DiscoverDbPrefix);

                if (sampleSize == 0)
                {
                    sampleSize = 5;
                }

                foreach (var rootPath in _server.Settings.RootPaths)
                {
                    var schemaName = Constants.SchemaName;
                    var tableName = string.IsNullOrWhiteSpace(rootPath.Name)
                        ? new DirectoryInfo(rootPath.RootPath).Name
                        : rootPath.Name;

                    Utility.LoadDirectoryFilesIntoDb(
                        Utility.GetImportExportFactory(rootPath.Mode), conn, rootPath,
                        tableName, schemaName, files[rootPath.RootPathName()], sampleSize, 1);
                }

                var schemas = refreshSchemas.Select(s => Discover.GetSchemaForQuery(s, sampleSize))
                    .ToArray();

                discoverSchemasResponse.Schemas.AddRange(schemas.Where(x => x != null));

                // return all schemas 
                Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");
                return discoverSchemasResponse;
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
                return new DiscoverSchemasResponse();
            }
            finally
            {
                DiscoverSemaphoreSlim.Release();
            }
        }

        /// <summary>
        /// Publishes a stream of data for a given schema
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task ReadStream(ReadRequest request, IServerStreamWriter<Record> responseStream,
            ServerCallContext context)
        {
            var schema = request.Schema;
            var limit = request.Limit;
            var limitFlag = request.Limit != 0;
            var jobId = request.JobId;
            var recordsCount = 0;

            Logger.SetLogPrefix(request.JobId);
            Logger.Info($"Publishing records for schema: {schema.Name}");

            try
            {
                var conn = Utility.GetSqlConnection(jobId);
                var filesByRootPath = _server.Settings.GetAllFilesByRootPath();

                if (string.IsNullOrWhiteSpace(schema.Query))
                {
                    // schema is not query based so we can stream each file as it is loaded
                    var rootPaths = _server.Settings.GetRootPathsFromQuery(Utility.GetDefaultQuery(schema));
                    var rootPath = rootPaths.First();
                    var files = filesByRootPath[rootPath.RootPathName()];
                    var schemaName = Constants.SchemaName;
                    var tableName = string.IsNullOrWhiteSpace(rootPath.Name)
                        ? new DirectoryInfo(rootPath.RootPath).Name
                        : rootPath.Name;
                    if (files.Count > 0)
                    {
                        // load file and then stream file one by one
                        foreach (var file in files)
                        {
                            Utility.LoadDirectoryFilesIntoDb(Utility.GetImportExportFactory(rootPath.Mode), conn,
                                rootPath,
                                tableName, schemaName, new List<string> {file});

                            var records = Read.ReadRecords(schema, jobId);

                            foreach (var record in records)
                            {
                                // stop publishing if the limit flag is enabled and the limit has been reached or the server is disconnected
                                if ((limitFlag && recordsCount == limit) || !_server.Connected)
                                {
                                    break;
                                }

                                // publish record
                                await responseStream.WriteAsync(record);
                                recordsCount++;
                            }
                        }
                    }
                    else
                    {
                        Utility.DeleteDirectoryFilesFromDb(conn, tableName, schemaName);
                    }
                }
                else
                {
                    // schema is query based
                    var rootPaths = _server.Settings.GetRootPathsFromQuery(schema.Query);

                    Logger.Info(
                        $"Query root paths {JsonConvert.SerializeObject(rootPaths.Select(r => r.RootPath).ToList(), Formatting.Indented)}");

                    // schema is query based so everything in query needs to be loaded first
                    foreach (var rootPath in rootPaths)
                    {
                        var files = filesByRootPath[rootPath.RootPathName()];
                        var schemaName = Constants.SchemaName;
                        var tableName = string.IsNullOrWhiteSpace(rootPath.Name)
                            ? new DirectoryInfo(rootPath.RootPath).Name
                            : rootPath.Name;
                        if (files.Count > 0)
                        {
                            Utility.LoadDirectoryFilesIntoDb(Utility.GetImportExportFactory(rootPath.Mode), conn,
                                rootPath,
                                tableName, schemaName, files);
                        }
                        else
                        {
                            Utility.DeleteDirectoryFilesFromDb(conn, tableName, schemaName);
                        }
                    }

                    var records = Read.ReadRecords(schema, jobId);

                    foreach (var record in records)
                    {
                        // stop publishing if the limit flag is enabled and the limit has been reached or the server is disconnected
                        if ((limitFlag && recordsCount == limit) || !_server.Connected)
                        {
                            break;
                        }

                        // publish record
                        await responseStream.WriteAsync(record);
                        recordsCount++;
                    }
                }

                Logger.Info($"Published {recordsCount} records");

                foreach (var rootPath in _server.Settings.RootPaths)
                {
                    var files = filesByRootPath[rootPath.RootPathName()];
                    switch (rootPath.CleanupAction)
                    {
                        case Constants.CleanupActionDelete:
                            foreach (var file in files)
                            {
                                Logger.Info($"Deleting file {file}");
                                Utility.DeleteFileAtPath(file, rootPath, _server.Settings, true);
                            }

                            break;
                        case Constants.CleanupActionArchive:
                            foreach (var file in files)
                            {
                                Logger.Info($"Archiving file {file} to {rootPath.ArchivePath}");
                                Utility.ArchiveFileAtPath(file, rootPath, _server.Settings);
                            }

                            break;
                    }

                    // clean up local files pulled from remote
                    if (rootPath.FileReadMode != Constants.FileModeLocal)
                    {
                        foreach (var file in files)
                        {
                            Logger.Info($"Source file is not local, deleting local file {file}");
                            Utility.DeleteFileAtPath(file, rootPath, _server.Settings, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.StackTrace, context);
            }
        }

        /// <summary>
        /// Configures writebacks to File
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ConfigureWriteResponse> ConfigureWrite(ConfigureWriteRequest request,
            ServerCallContext context)
        {
            Logger.SetLogPrefix("configure_write");
            Logger.Info("Configuring write...");
            Logger.Debug(JsonConvert.SerializeObject(request, Formatting.Indented));

            var schemaJson = Write.GetSchemaJson();
            var uiJson = Write.GetUIJson();

            // if first call 
            if (string.IsNullOrWhiteSpace(request.Form.DataJson) || request.Form.DataJson == "{}")
            {
                Logger.Info("first call...");
                return Task.FromResult(new ConfigureWriteResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = "",
                        DataErrorsJson = "",
                        Errors = { },
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = ""
                    },
                    Schema = null
                });
            }

            try
            {
                Logger.Info("second call...");
                // get form data
                var formData = JsonConvert.DeserializeObject<ConfigureWriteFormData>(request.Form.DataJson);
                formData.ConvertLegacyConfiguration();
                var errors = formData.ValidateWriteFormData(_server.Settings);

                // base schema to return
                var schema = Write.GetSchemaFromForm(formData);

                return Task.FromResult(new ConfigureWriteResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = request.Form.DataJson,
                        Errors = {errors},
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = request.Form.StateJson
                    },
                    Schema = schema
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
                return Task.FromResult(new ConfigureWriteResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = request.Form.DataJson,
                        Errors = {e.Message},
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = request.Form.StateJson
                    },
                    Schema = null
                });
            }
        }

        /// <summary>
        /// Configures replication writebacks to File
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ConfigureReplicationResponse> ConfigureReplication(ConfigureReplicationRequest request,
            ServerCallContext context)
        {
            Logger.SetLogPrefix("configure_replication");
            Logger.Info($"Configuring write for schema name {request?.Schema?.Name}...");

            var schemaJson = Replication.GetSchemaJson();
            var uiJson = Replication.GetUIJson();

            try
            {
                if (!string.IsNullOrWhiteSpace(request.Form.DataJson))
                {
                    // check for config errors
                    var replicationFormData =
                        JsonConvert.DeserializeObject<ConfigureReplicationFormData>(request.Form.DataJson);
                    replicationFormData.ConvertLegacyConfiguration();
                    var errors = replicationFormData.ValidateReplicationFormData(_server.Settings);

                    return Task.FromResult(new ConfigureReplicationResponse
                    {
                        Form = new ConfigurationFormResponse
                        {
                            DataJson = JsonConvert.SerializeObject(replicationFormData),
                            Errors = {errors},
                            SchemaJson = schemaJson,
                            UiJson = uiJson,
                            StateJson = request.Form.StateJson
                        }
                    });
                }

                return Task.FromResult(new ConfigureReplicationResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = request.Form.DataJson,
                        Errors = { },
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = request.Form.StateJson
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
                return Task.FromResult(new ConfigureReplicationResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = request.Form.DataJson,
                        Errors = {e.Message},
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = request.Form.StateJson
                    }
                });
            }
        }

        /// <summary>
        /// Prepares writeback settings to write to file
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task<PrepareWriteResponse> PrepareWrite(PrepareWriteRequest request,
            ServerCallContext context)
        {
            // Logger.SetLogLevel(Logger.LogLevel.Debug);
            Logger.SetLogPrefix(request.DataVersions.JobId);
            Logger.Info("Preparing write...");
            Logger.Debug(JsonConvert.SerializeObject(request, Formatting.Indented));
            _server.WriteConfigured = false;

            var conn = Utility.GetSqlConnection(request.DataVersions.JobId, true, _server?.Config?.PermanentDirectory);

            _server.WriteSettings = new WriteSettings
            {
                CommitSLA = request.CommitSlaSeconds,
                Schema = request.Schema,
                Replication = request.Replication,
                DataVersions = request.DataVersions,
                Connection = conn,
            };

            if (_server.WriteSettings.IsReplication())
            {
                try
                {
                    // setup import export helpers
                    var factory = Utility.GetImportExportFactory(Constants.ModeDelimited);
                    var replicationSettings =
                        JsonConvert.DeserializeObject<ConfigureReplicationFormData>(request.Replication.SettingsJson);
                    replicationSettings.ConvertLegacyConfiguration();
                    _server.WriteSettings.GoldenImportExport = factory.MakeImportExportFile(conn, replicationSettings,
                        replicationSettings.GetGoldenTableName(), Constants.SchemaName);
                    _server.WriteSettings.VersionImportExport = factory.MakeImportExportFile(conn, replicationSettings,
                        replicationSettings.GetVersionTableName(), Constants.SchemaName);

                    // prepare write locations
                    Directory.CreateDirectory(replicationSettings.GetLocalGoldenDirectory());
                    Directory.CreateDirectory(replicationSettings.GetLocalVersionDirectory());
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message, context);
                    return new PrepareWriteResponse();
                }

                // reconcile job
                Logger.Info($"Starting to reconcile Replication Job {request.DataVersions.JobId}");
                try
                {
                    await Replication.ReconcileReplicationJobAsync(_server.WriteSettings.Connection, request);
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message, context);
                    return new PrepareWriteResponse();
                }

                Logger.Info($"Finished reconciling Replication Job {request.DataVersions.JobId}");
            }
            else
            {
                try
                {
                    // setup import export helper
                    var factory = Utility.GetImportExportFactory(Constants.ModeDelimited);
                    var writeFormData =
                        JsonConvert.DeserializeObject<ConfigureWriteFormData>(request.Schema.PublisherMetaJson);
                    writeFormData.ConvertLegacyConfiguration();
                    _server.WriteSettings.TargetImportExport = factory.MakeImportExportFile(conn, writeFormData,
                        writeFormData.GetTargetTableName(), Constants.SchemaName);

                    // prepare write location
                    Directory.CreateDirectory(writeFormData.GetLocalTargetDirectory());

                    // reconcile job
                    Logger.Info($"Starting to reconcile Replication Job {request.DataVersions.JobId}");

                    request.Replication = new ReplicationWriteRequest
                    {
                        SettingsJson = JsonConvert.SerializeObject(writeFormData.GetReplicationFormData())
                    };

                    await Replication.ReconcileReplicationJobAsync(_server.WriteSettings.Connection, request);

                    Logger.Info($"Finished reconciling Replication Job {request.DataVersions.JobId}");
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message, context);
                    return new PrepareWriteResponse();
                }
            }

            _server.WriteConfigured = true;

            // Logger.Debug(JsonConvert.SerializeObject(_server.WriteSettings, Formatting.Indented));
            Logger.Info("Write prepared.");
            return new PrepareWriteResponse();
        }

        /// <summary>
        /// Writes records to files
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task WriteStream(IAsyncStreamReader<Record> requestStream,
            IServerStreamWriter<RecordAck> responseStream, ServerCallContext context)
        {
            try
            {
                Logger.Info("Writing records to File...");

                var schema = _server.WriteSettings.Schema;
                var inCount = 0;

                if (_server.WriteSettings.IsReplication())
                {
                    var config =
                        JsonConvert.DeserializeObject<ConfigureReplicationFormData>(_server.WriteSettings
                            .Replication
                            .SettingsJson);
                    config.ConvertLegacyConfiguration();
                    
                    try
                    {
                        // watcher to periodically write file to disk
                        // SHOULD NOT BE AWAITED
                        Task.Run(() =>
                        {
                            while (_server.Connected)
                            {
                                Replication.WriteToDisk(_server.WriteSettings.GoldenImportExport,
                                    _server.WriteSettings.VersionImportExport, config, _server.Settings);
                                Thread.Sleep(1000);
                            }
                        });

                        while (await requestStream.MoveNext(context.CancellationToken) && _server.Connected &&
                               _server.WriteConfigured)
                        {
                            var record = requestStream.Current;
                            inCount++;

                            // send record to source system
                            // add await for unit testing 
                            // removed to allow multiple to run at the same time
                            await Task.Run(
                                async () => await Replication.WriteRecordAsync(_server.WriteSettings.Connection, schema,
                                    record, config,
                                    responseStream), context.CancellationToken);
                        }
                    }
                    finally
                    {
                        // write any pending records to file
                        Replication.WriteToDisk(_server.WriteSettings.GoldenImportExport,
                            _server.WriteSettings.VersionImportExport, config, _server.Settings, true);
                    }

                }
                else
                {
                    var writeConfig = JsonConvert.DeserializeObject<ConfigureWriteFormData>(schema.PublisherMetaJson);
                    var config = writeConfig.GetReplicationFormData();
                    config.ConvertLegacyConfiguration();

                    try
                    {
                        // watcher to periodically write file to disk
                        // SHOULD NOT BE AWAITED
                        Task.Run(() =>
                        {
                            while (_server.Connected)
                            {
                                Write.WriteToDisk(_server.WriteSettings.TargetImportExport, writeConfig,
                                    _server.Settings);
                                Thread.Sleep(1000);
                            }
                        });

                        while (await requestStream.MoveNext(context.CancellationToken) && _server.Connected &&
                               _server.WriteConfigured)
                        {
                            var record = requestStream.Current;
                            inCount++;

                            // send record to source system
                            // add await for unit testing 
                            // removed to allow multiple to run at the same time
                            await Task.Run(
                                async () => await Write.WriteRecordAsync(_server.WriteSettings.Connection, schema,
                                    record, config,
                                    responseStream), context.CancellationToken);
                        }
                    }
                    finally
                    {
                        // write any pending records to file
                        Write.WriteToDisk(_server.WriteSettings.TargetImportExport, writeConfig, _server.Settings, true);
                    }
                }
                
                Logger.Info($"Wrote {inCount} records to File.");
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message, context);
                throw;
            }
        }

        /// <summary>
        /// Handles disconnect requests from the agent
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<DisconnectResponse> Disconnect(DisconnectRequest request, ServerCallContext context)
        {
            // clear connection
            _server.Connected = false;
            _server.Settings = null;
            _server.WriteSettings = null;

            // alert connection session to close
            if (_tcs != null)
            {
                _tcs.SetResult(true);
                _tcs = null;
            }

            Logger.Info("Disconnected");
            return Task.FromResult(new DisconnectResponse());
        }
    }
}