using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Factory.Implementations.AS400;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using Xunit;
using Record = Naveego.Sdk.Plugins.Record;

namespace PluginFileReaderTest.Plugin
{
    public class AS400PluginTest
    {
        private readonly string DiscoverDatabasePath = $"{Path.Join(Constants.DbFolder, $"{Constants.DiscoverDbPrefix}_{Constants.DbFile}")}";
        private readonly string ReadDatabasePath = $"{Path.Join(Constants.DbFolder, $"test_{Constants.DbFile}")}";
        private const string BasePath = "../../../MockData/AS400Data";
        private const string ReadPath = "../../../MockData/ReadDirectory";
        private const string ReadDifferentPath = "../../../MockData/ReadDirectoryDifferent";
        private const string ArchivePath = "../../../MockData/ArchiveDirectory";
        private const string FormatConfigFilePath = "../../../MockData/Configuration/VEH_POL_FORMAT.json";
        private const string TestTableName = "PrimaryDirectory";
        private const string DefaultCleanupAction = "none";
        private const string DefaultFilter = "*.txt";
        private const string AS400Mode = "AS400";

        private void PrepareTestEnvironment(bool configureInvalid)
        {
            try
            {
                File.Delete(DiscoverDatabasePath);
            }
            catch
            {
            }

            try
            {
                File.Delete(ReadDatabasePath);
            }
            catch 
            {
            }
            

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
            }
        }

        private Settings GetSettings(string cleanupAction = null, string delimiter = ",",
            string filter = null, bool multiRoot = false)
        {
            return new Settings
            {
                RootPaths = multiRoot
                    ? new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = ReadPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = AS400Mode,
                            Delimiter = delimiter,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            Columns = null,
                            ModeSettings = new ModeSettings
                            {
                                AS400Settings = new AS400Settings
                                {
                                    KeyValueWidth = 2,
                                    AS400FormatsConfigurationFile = FormatConfigFilePath
                                }
                            }
                        },
                        new RootPathObject
                        {
                            RootPath = ReadDifferentPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = AS400Mode,
                            Delimiter = delimiter,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            Columns = null,
                            ModeSettings = new ModeSettings
                            {
                                AS400Settings = new AS400Settings
                                {
                                    KeyValueWidth = 2,
                                    AS400FormatsConfigurationFile = FormatConfigFilePath
                                }
                            }
                        }
                    }
                    : new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = ReadPath,
                            Filter = filter ?? DefaultFilter,
                            Delimiter = delimiter,
                            Mode = AS400Mode,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            Columns = null,
                            ModeSettings = new ModeSettings
                            {
                                AS400Settings = new AS400Settings
                                {
                                    KeyValueWidth = 2,
                                    AS400FormatsConfigurationFile = FormatConfigFilePath
                                }
                            }
                        }
                    }
            };
        }

        private ConnectRequest GetConnectSettings(string cleanupAction = null, string delimiter = ",",
            string filter = null, bool multiRoot = false)
        {
            var settings = GetSettings(cleanupAction, delimiter, filter, multiRoot);

            return new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(settings),
                OauthConfiguration = new OAuthConfiguration(),
                OauthStateJson = ""
            };
        }

        private Schema GetTestSchema(string query)
        {
            return new Schema
            {
                Id = "test",
                Name = "test",
                Query = query
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
        public async Task DiscoverSchemasAllTest()
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

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectory_VEH_POL]", schema.Id);
            Assert.Equal("ReadDirectory_VEH_POL", schema.Name);
            Assert.Equal($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory_VEH_POL]", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(138, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("VEH.NUM", property.Id);
            Assert.Equal("VEH.NUM", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasRefreshTest()
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

            var connectRequest = GetConnectSettings();

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory_VEH_POL]")},
            };

            // act
            client.Connect(connectRequest);
            client.DiscoverSchemas(discoverAllRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

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

            var connectRequest = GetConnectSettings();

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings();
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory_VEH_POL]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                RootPath = settings.RootPaths.First()
            });

            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Connect(connectRequest);
            var schemasResponse = client.DiscoverSchemas(schemaRequest);
            request.Schema = schemasResponse.Schemas[0];
            
            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            // Assert.Equal(31032, records.Count);
            Assert.Equal(3443, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("0001", record["VEH.NUM"]);
            Assert.Equal("2003", record["VEH.YR"]);
            Assert.Equal("ADDPIP", record["COVCODE"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ReadStreamLimitTest()
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

            var connectRequest = GetConnectSettings();

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };
            
            var settings = GetSettings();
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory_VEH_POL]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                RootPath = settings.RootPaths.First()
            });

            var request = new ReadRequest()
            {
                Schema = schema,
                Limit = 10,
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Connect(connectRequest);
            client.DiscoverSchemas(discoverAllRequest);
            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(10, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}