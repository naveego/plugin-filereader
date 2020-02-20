using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Discover;
using PluginFileReader.API.Read;
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

        public Plugin()
        {
            _server = new ServerStatus();
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

            Logger.Info("Connecting...");

            var settings = JsonConvert.DeserializeObject<Settings>(request.SettingsJson);

            // validate settings passed in
            try
            {
                _server.Settings = settings;
                _server.Settings.Validate();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return Task.FromResult(new ConnectResponse
                {
                    OauthStateJson = request.OauthStateJson,
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
        public override Task<DiscoverSchemasResponse> DiscoverSchemas(DiscoverSchemasRequest request,
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
                    var files = _server.Settings.GetAllFilesByDirectory();
                    Logger.Info($"Schemas attempted: {files.Count}");

                    var schemas = _server.Settings.RootPaths.Select(p =>
                            Discover.GetSchemaForDirectory(Utility.GetImportExportFactory(p), p, files[p.RootPath],
                                sampleSize))
                        .ToArray();

                    discoverSchemasResponse.Schemas.AddRange(schemas.Where(x => x != null));

                    Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");

                    return Task.FromResult(discoverSchemasResponse);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    throw;
                }
            }

            try
            {
                var refreshSchemas = request.ToRefresh;

                Logger.Info($"Refresh schemas attempted: {refreshSchemas.Count}");

                var schemas = refreshSchemas.Select(s => Discover.GetSchemaForQuery(s, sampleSize))
                    .ToArray();

                discoverSchemasResponse.Schemas.AddRange(schemas.Where(x => x != null));

                // return all schemas 
                Logger.Info($"Schemas returned: {discoverSchemasResponse.Schemas.Count}");
                return Task.FromResult(discoverSchemasResponse);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
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
                var filesByDirectory = _server.Settings.GetAllFilesByDirectory();

                if (schema.PublisherMetaJson != "")
                {
                    // schema is not query based so we can stream each file as it is loaded
                    var schemaMetaJson = JsonConvert.DeserializeObject<SchemaPublisherMetaJson>(schema.PublisherMetaJson);
                    var files = filesByDirectory[schemaMetaJson.RootPath.RootPath];
                    var schemaName = Constants.SchemaName;
                    var tableName = new DirectoryInfo(schemaMetaJson.RootPath.RootPath).Name;
                    if (files.Count > 0)
                    {
                        // load file and then stream file one by one
                        foreach (var file in files)
                        {
                            Utility.LoadDirectoryFilesIntoDb(Utility.GetImportExportFactory(schemaMetaJson.RootPath), conn, schemaMetaJson.RootPath,
                                tableName, schemaName, new List<string>{file});
                                
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
                    // schema is query based so everything needs to be loaded first
                    foreach (var rootPath in _server.Settings.RootPaths)
                    {
                        var files = filesByDirectory[rootPath.RootPath];
                        var schemaName = Constants.SchemaName;
                        var tableName = new DirectoryInfo(rootPath.RootPath).Name;
                        if (files.Count > 0)
                        {
                            Utility.LoadDirectoryFilesIntoDb(Utility.GetImportExportFactory(rootPath), conn, rootPath,
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
                    var files = filesByDirectory[rootPath.RootPath];
                    switch (rootPath.CleanupAction)
                    {
                        case "Delete":
                            foreach (var file in files)
                            {
                                Logger.Info($"Deleting file {file}");
                                Utility.DeleteFileAtPath(file);
                            }

                            break;
                        case "Archive":
                            foreach (var file in files)
                            {
                                Logger.Info($"Archiving file {file} to {rootPath.ArchivePath}");
                                Utility.ArchiveFileAtPath(file, rootPath.ArchivePath);
                            }

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates a form and handles form updates for write backs
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<ConfigureWriteResponse> ConfigureWrite(ConfigureWriteRequest request,
            ServerCallContext context)
        {
            Logger.Info("Configuring write...");


            var schemaJson = Write.GetSchemaJson();
            var uiJson = Write.GetUIJson();

            // if first call 
            if (request.Form == null || request.Form.DataJson == "")
            {
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
                // get form data
                var formData = JsonConvert.DeserializeObject<ConfigureWriteFormData>(request.Form.DataJson);

                // base schema to return
                var schema = new Schema
                {
                    Id = "",
                    Name = "",
                    Query = formData.Query,
                    DataFlowDirection = Schema.Types.DataFlowDirection.Write
                };

                // add parameters to properties
                foreach (var param in formData.Parameters)
                {
                    schema.Properties.Add(new Property
                    {
                        Id = param.ParamName,
                        Name = param.ParamName,
                        Type = Write.GetWritebackType(param.ParamType)
                    });
                }

                return Task.FromResult(new ConfigureWriteResponse
                {
                    Form = new ConfigurationFormResponse
                    {
                        DataJson = request.Form.DataJson,
                        Errors = { },
                        SchemaJson = schemaJson,
                        UiJson = uiJson,
                        StateJson = request.Form.StateJson
                    },
                    Schema = schema
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
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
        /// Prepares the plugin to handle a write request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<PrepareWriteResponse> PrepareWrite(PrepareWriteRequest request, ServerCallContext context)
        {
            Logger.Info("Preparing write...");
            _server.WriteConfigured = false;

            var writeSettings = new WriteSettings
            {
                CommitSLA = request.CommitSlaSeconds,
                Schema = request.Schema
            };

            _server.WriteSettings = writeSettings;
            _server.WriteConfigured = true;

            Logger.Info("Write prepared.");
            return Task.FromResult(new PrepareWriteResponse());
        }

        /// <summary>
        /// Takes in records and writes them out then sends acks back to the client
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task WriteStream(IAsyncStreamReader<Record> requestStream,
            IServerStreamWriter<RecordAck> responseStream, ServerCallContext context)
        {
            // try
            // {
            //     Logger.Info("Writing records...");
            //     var schema = _server.WriteSettings.Schema;
            //     var sla = _server.WriteSettings.CommitSLA;
            //     var inCount = 0;
            //     var outCount = 0;
            //
            //     // get next record to publish while connected and configured
            //     while (await requestStream.MoveNext(context.CancellationToken) && _server.Connected &&
            //            _server.WriteConfigured)
            //     {
            //         var record = requestStream.Current;
            //         inCount++;
            //         
            //         Logger.Debug($"Got Record {record.DataJson}");
            //
            //         // send record to source system
            //         // timeout if it takes longer than the sla
            //         var task = Task.Run(() => PutRecord(schema, record));
            //         if (task.Wait(TimeSpan.FromSeconds(sla)))
            //         {
            //             // send ack
            //             var ack = new RecordAck
            //             {
            //                 CorrelationId = record.CorrelationId,
            //                 Error = task.Result
            //             };
            //             await responseStream.WriteAsync(ack);
            //
            //             if (String.IsNullOrEmpty(task.Result))
            //             {
            //                 outCount++;
            //             }
            //         }
            //         else
            //         {
            //             // send timeout ack
            //             var ack = new RecordAck
            //             {
            //                 CorrelationId = record.CorrelationId,
            //                 Error = "timed out"
            //             };
            //             await responseStream.WriteAsync(ack);
            //         }
            //     }
            //
            //     Logger.Info($"Wrote {outCount} of {inCount} records.");
            // }
            // catch (Exception e)
            // {
            //     Logger.Error(e.Message);
            //     throw;
            // }
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