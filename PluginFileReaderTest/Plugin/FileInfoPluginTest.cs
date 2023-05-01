using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.Helper;
using Xunit;
using Record = Naveego.Sdk.Plugins.Record;

namespace PluginFileReaderTest.Plugin
{
    public class FileInfoPluginTest
    {
        private const string BasePath = "../../../MockData/XMLData";
        private const string ReadPath = "../../../MockData/ReadDirectory";
        private const string ReadDifferentPath = "../../../MockData/ReadDirectoryDifferent";
        private const string ArchivePath = "../../../MockData/ArchiveDirectory";
        private const string ReplicationPath = "../../../MockData/ReplicationDirectory";
        private const string TargetWriteFile = "target.csv";
        private const string DefaultCleanupAction = "none";
        private const string DefaultFilter = "*.xml";
        private const string FileCopyMode = "File Copy";

        private void PrepareTestEnvironment(bool configureInvalid = false, bool configureArchiveFull = false,
            bool configureEmpty = false)
        {
            Directory.CreateDirectory(ArchivePath);
            Directory.CreateDirectory(ReadPath);
            Directory.CreateDirectory(ReadDifferentPath);

            foreach (var filePath in Directory.GetFiles(ArchivePath))
            {
                File.Delete(filePath);
            }

            foreach (var filePath in Directory.GetFiles(ReadPath))
            {
                File.Delete(filePath);
            }

            foreach (var filePath in Directory.GetFiles(ReadDifferentPath))
            {
                File.Delete(filePath);
            }

            if (!configureEmpty)
            {
                foreach (var filePath in Directory.GetFiles(BasePath))
                {
                    var targetPath = "";
                    if (configureInvalid)
                    {
                        targetPath = $"{Path.Join(ReadPath, Path.GetFileName(filePath))}";
                    }
                    else
                    {
                        targetPath = filePath.Contains("DIFFERENT")
                            ? $"{Path.Join(ReadDifferentPath, Path.GetFileName(filePath))}"
                            : $"{Path.Join(ReadPath, Path.GetFileName(filePath))}";
                    }

                    File.Copy(filePath, targetPath, true);

                    if (configureArchiveFull)
                    {
                        targetPath = $"{Path.Join(ArchivePath, Path.GetFileName(filePath))}";

                        File.Copy(filePath, targetPath, true);
                    }
                }
            }
        }

        private Settings GetSettings(string cleanupAction = null, int skipLines = 0,
            string filter = null, bool multiRoot = false, bool sftp = false, int delayMS = 0, string oldRegexReplace = "", string newRegexReplace = "")
        {
            return new Settings
            {
                FtpHostname = "",
                FtpPort = 22,
                FtpUsername = "",
                FtpPassword = "",
                RootPaths = multiRoot
                    ? new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = sftp ? "" : ReadPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = FileCopyMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            FileReadMode = sftp ? "SFTP" : "Local",
                            ModeSettings = new ModeSettings
                            {
                                FileCopySettings = new FileCopySettings
                                {
                                    FtpHostname = "",
                                    FtpPort = 22,
                                    FtpUsername = "",
                                    FtpPassword = "",
                                    FtpSshKey = @"",
                                    TargetFileMode = "SFTP",
                                    TargetDirectoryPath = "",
                                    OverwriteTarget = true,
                                    MinimumSendDelayMS = delayMS,
                                    OldRegexReplace = oldRegexReplace,
                                    NewRegexReplace = newRegexReplace
                                }
                            }
                        },
                        new RootPathObject
                        {
                            RootPath = sftp ? "" : ReadDifferentPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = FileCopyMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            FileReadMode = sftp ? "SFTP" : "Local",
                            ModeSettings = new ModeSettings
                            {
                                FileCopySettings = new FileCopySettings
                                {
                                    FtpHostname = "",
                                    FtpPort = 22,
                                    FtpUsername = "",
                                    FtpPassword = "",
                                    FtpSshKey = @"",
                                    TargetFileMode = "SFTP",
                                    TargetDirectoryPath = "",
                                    OverwriteTarget = true,
                                    MinimumSendDelayMS = delayMS,
                                    OldRegexReplace = oldRegexReplace,
                                    NewRegexReplace = newRegexReplace
                                }
                            }
                        }
                    }
                    : new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = sftp ? "" : ReadPath,
                            Filter = filter ?? DefaultFilter,
                            SkipLines = skipLines,
                            Mode = FileCopyMode,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            FileReadMode = sftp ? "SFTP" : "Local",
                            ModeSettings = new ModeSettings
                            {
                                FileCopySettings = new FileCopySettings
                                {
                                    FtpHostname = "",
                                    FtpPort = 22,
                                    FtpUsername = "",
                                    FtpPassword = "",
                                    FtpSshKey = @"",
                                    TargetFileMode = sftp ? "SFTP" : "Local",
                                    TargetDirectoryPath = sftp ? "" : ArchivePath,
                                    OverwriteTarget = true,
                                    MinimumSendDelayMS = delayMS,
                                    OldRegexReplace = oldRegexReplace,
                                    NewRegexReplace = newRegexReplace
                                }
                            }
                        }
                    }
            };
        }

        private ConnectRequest GetConnectSettings(string cleanupAction = null, int skipLines = 0,
            string filter = null, bool multiRoot = false, bool empty = false, bool sftp = false, int delayMS = 0, string oldRegexReplace = "", string newRegexReplace = "")
        {
            if (empty)
            {
                var emptySettings = new Settings();

                return new ConnectRequest
                {
                    SettingsJson = JsonConvert.SerializeObject(emptySettings),
                    OauthConfiguration = new OAuthConfiguration(),
                    OauthStateJson = ""
                };
            }

            var settings = GetSettings(cleanupAction, skipLines, filter, multiRoot, sftp);

            return new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(settings),
                OauthConfiguration = new OAuthConfiguration(),
                OauthStateJson = ""
            };
        }

        private Schema GetTestSchema(string query = "")
        {
            return new Schema
            {
                Id = "test",
                Name = "test",
                Query = query,
            };
        }

        [Fact]
        public async Task ConnectSessionTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();
            var disconnectRequest = new DisconnectRequest();

            // act
            var response = client.ConnectSession(request);
            var responseStream = response.ResponseStream;
            var records = new List<ConnectResponse>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
                client.Disconnect(disconnectRequest);
            }

            // assert
            Assert.Single(records);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ConnectTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings();

            // act
            var response = client.Connect(request);

            // assert
            Assert.IsType<ConnectResponse>(response);
            Assert.Equal("", response.SettingsError);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ReadStreamTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);
            
            var configureRequest = new ConfigureRequest
            {
                TemporaryDirectory = "../../../Temp",
                PermanentDirectory = "../../../Perm",
                LogDirectory = "../../../Logs",
                DataVersions = new DataVersions(),
                LogLevel = LogLevel.Debug
            };

            var connectRequest = GetConnectSettings(null, 0, "*", false, false, false);

            var discoverRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };
            
            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act - find AU_FileInformation schema, then read table
            client.Configure(configureRequest);
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(discoverRequest);

            var fileInfoSchema = schemasResponse.Schemas.First(s => FileInfoData.IsFileInfoSchema(s));
            Assert.Equal(fileInfoSchema.Id, FileInfoData.FileInfoSchemaId);
            Assert.Equal(fileInfoSchema.Name, FileInfoData.FileInfoSchemaId);
            Assert.True(string.IsNullOrWhiteSpace(fileInfoSchema.Query));
            Assert.Equal(fileInfoSchema.DataFlowDirection, Schema.Types.DataFlowDirection.Read);
            Assert.Equal(fileInfoSchema.Properties.Count, FileInfoData.FileInfoProperties.Count);

            request.Schema = fileInfoSchema;

            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(3, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal(record["RootPath"], "");
            Assert.Equal(record["FileName"], "");

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ReadStreamRenameTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);
            
            var configureRequest = new ConfigureRequest
            {
                TemporaryDirectory = "../../../Temp",
                PermanentDirectory = "../../../Perm",
                LogDirectory = "../../../Logs",
                DataVersions = new DataVersions(),
                LogLevel = LogLevel.Debug
            };

            var connectRequest = GetConnectSettings(null, 0, "*", false, false, false, 0, "^(.*)$", "part-$1");

            var discoverRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };
            
            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Configure(configureRequest);
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(discoverRequest);
            request.Schema = schemasResponse.Schemas[0];

            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(3, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("True", record["RUN_SUCCESS"]);
            Assert.Equal("", record["RUN_ERROR"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamSftpTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);
            
            var configureRequest = new ConfigureRequest
            {
                TemporaryDirectory = "../../../Temp",
                PermanentDirectory = "../../../Perm",
                LogDirectory = "../../../Logs",
                DataVersions = new DataVersions(),
                LogLevel = LogLevel.Debug
            };

            var connectRequest = GetConnectSettings(null, 0, "*", false, false, true);

            var discoverRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };
            
            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Configure(configureRequest);
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(discoverRequest);
            request.Schema = schemasResponse.Schemas[0];

            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(1, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("True", record["RUN_SUCCESS"]);
            Assert.Equal("", record["RUN_ERROR"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}