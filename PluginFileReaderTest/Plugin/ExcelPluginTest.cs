using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using Xunit;
using Record = Naveego.Sdk.Plugins.Record;

namespace PluginFileReaderTest.Plugin
{
    public class ExcelPluginTest
    {
        private const string BasePath = "../../../MockData/ExcelData";
        private const string ReadPath = "../../../MockData/ReadDirectory";
        private const string ReadDifferentPath = "../../../MockData/ReadDirectoryDifferent";
        private const string ArchivePath = "../../../MockData/ArchiveDirectory";
        private const string ReplicationPath = "../../../MockData/ReplicationDirectory";
        private const string TargetWriteFile = "target.csv";
        private const string GoldenReplicationFile = "golden.csv";
        private const string VersionReplicationFile = "version.csv";
        private const string DefaultCleanupAction = "none";
        private const string DefaultFilter = "*.xls";
        private const string ExcelMode = "Excel";

        private void PrepareTestEnvironment(bool configureInvalid = false, bool configureArchiveFull = false,
            bool configureEmpty = false)
        {
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
            string filter = null, bool multiRoot = false, string excelColumns = null, List<ExcelCell> excelCells = null)
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
                            Mode = ExcelMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            ExcelColumns = excelColumns,
                            ExcelCells = excelCells
                        },
                        new RootPathObject
                        {
                            RootPath = ReadDifferentPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = ExcelMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            ExcelColumns = excelColumns,
                            ExcelCells = excelCells
                        }
                    }
                    : new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = ReadPath,
                            Filter = filter ?? DefaultFilter,
                            SkipLines = skipLines,
                            Mode = ExcelMode,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            ExcelColumns = excelColumns,
                            ExcelCells = excelCells
                        }
                    }
            };
        }

        private ConnectRequest GetConnectSettings(string cleanupAction = null, int skipLines = 0,
            string filter = null, bool multiRoot = false, bool empty = false, string excelColumns = null,
            List<ExcelCell> excelCells = null)
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

            var settings = GetSettings(cleanupAction, skipLines, filter, multiRoot, excelColumns, excelCells);

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

            var connectRequest = GetConnectSettings(null, 8);

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
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectory]", schema.Id);
            Assert.Equal("ReadDirectory", schema.Name);
            Assert.Equal($"", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(10, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("HCPCS Code", property.Id);
            Assert.Equal("HCPCS Code", property.Name);
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

            var connectRequest = GetConnectSettings(null, 8);

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]")},
            };

            // act
            client.Connect(connectRequest);
            client.DiscoverSchemas(discoverAllRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal($"test", schema.Id);
            Assert.Equal("test", schema.Name);
            Assert.Equal($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(5, schema.Sample.Count);
            Assert.Equal(10, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("HCPCS Code", property.Id);
            Assert.Equal("HCPCS Code", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasExcelColumnsTest()
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

            var connectRequest = GetConnectSettings(null, 8, null, false, false, "0, 2-3, 5, 7");

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
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectory]", schema.Id);
            Assert.Equal("ReadDirectory", schema.Name);
            Assert.Equal($"", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(5, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("HCPCS Code", property.Id);
            Assert.Equal("HCPCS Code", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

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

            var connectRequest = GetConnectSettings(null, 8);

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings();
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
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
            Assert.Equal(616, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("90371", record["HCPCS Code"]);
            Assert.Equal("Hep b ig im", record["Short Description"]);
            Assert.Equal("1 ML", record["HCPCS Code Dosage"]);
            Assert.Equal("115.892", record["Payment Limit"]);
            Assert.Equal("", record["Vaccine AWP%"]);
            Assert.Equal("", record["Vaccine Limit"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamExcelCellsTest()
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

            var connectRequest = GetConnectSettings(null, 8, "*.xls", false, false, null, new List<ExcelCell>
            {
                new ExcelCell
                {
                    ColumnIndex = 0,
                    RowIndex = 2,
                    ColumnName = "Special Column"
                }
            });

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings();
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
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
            Assert.Equal(616, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("90371", record["HCPCS Code"]);
            Assert.Equal("Hep b ig im", record["Short Description"]);
            Assert.Equal("1 ML", record["HCPCS Code Dosage"]);
            Assert.Equal("115.892", record["Payment Limit"]);
            Assert.Equal("", record["Vaccine AWP%"]);
            Assert.Equal("", record["Vaccine Limit"]);
            Assert.Equal("Effective July 1, 2020 through September 30, 2020", record["Special Column"]);

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

            var connectRequest = GetConnectSettings(null, 8);

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings();
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
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