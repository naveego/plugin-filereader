using System;
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
    public class FixedWidthColumnsPluginTest
    {
        private const string BasePath = "../../../MockData/FixedWidthColumnsData";
        private const string ReadPath = "../../../MockData/ReadDirectory";
        private const string ReadDifferentPath = "../../../MockData/ReadDirectoryDifferent";
        private const string ArchivePath = "../../../MockData/ArchiveDirectory";
        private const string ColumnsConfigFilePath = "../../../MockData/Configuration/ReadDirectoryConfig.json";
        private const string GlobalColumnsConfigFilePath = "../../../MockData/Configuration/GlobalConfig.json";
        private const string TestTableName = "PrimaryDirectory";
        private const string DefaultCleanupAction = "none";
        private const string DefaultFilter = "*.txt";
        private const string FixedWidthColumnsMode = "Fixed Width Columns";

        private void PrepareTestEnvironment(bool configureInvalid)
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
                            Mode = FixedWidthColumnsMode,
                            Delimiter = delimiter,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            Columns = new List<Column>
                            {
                                new Column()
                                {
                                    ColumnName = "id",
                                    ColumnStart = 0,
                                    ColumnEnd = 40,
                                    IsKey = true
                                },
                                new Column()
                                {
                                    ColumnName = "data1",
                                    ColumnStart = 44,
                                    ColumnEnd = 46,
                                    IsKey = false
                                },
                                new Column()
                                {
                                    ColumnName = "date1",
                                    ColumnStart = 47,
                                    ColumnEnd = 56,
                                    IsKey = false
                                },
                                new Column()
                                {
                                    ColumnName = "date2",
                                    ColumnStart = 57,
                                    ColumnEnd = 66,
                                    IsKey = false
                                },
                                new Column()
                                {
                                    ColumnName = "data2",
                                    ColumnStart = 67,
                                    ColumnEnd = 76,
                                    IsKey = false
                                },
                                new Column()
                                {
                                    ColumnName = "data3",
                                    ColumnStart = 276,
                                    ColumnEnd = 297,
                                    IsKey = false
                                }
                            }
                        },
                        new RootPathObject
                        {
                            RootPath = ReadDifferentPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = FixedWidthColumnsMode,
                            Delimiter = delimiter,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            Columns = new List<Column>
                            {
                                new Column()
                                {
                                    ColumnName = "id",
                                    ColumnStart = 0,
                                    ColumnEnd = 34,
                                    IsKey = true
                                },
                                new Column()
                                {
                                    ColumnName = "data1",
                                    ColumnStart = 36,
                                    ColumnEnd = 46,
                                    IsKey = false
                                },
                                new Column()
                                {
                                    ColumnName = "date1",
                                    ColumnStart = 47,
                                    ColumnEnd = 56,
                                    IsKey = false
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
                            Mode = FixedWidthColumnsMode,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            Columns = new List<Column>
                            {
                                new Column()
                                {
                                    ColumnName = "id",
                                    ColumnStart = 0,
                                    ColumnEnd = 43,
                                    IsKey = true,
                                    TrimWhitespace = true
                                },
                                new Column()
                                {
                                    ColumnName = "data1",
                                    ColumnStart = 44,
                                    ColumnEnd = 45,
                                    IsKey = false
                                },
                                new Column()
                                {
                                    ColumnName = "date1",
                                    ColumnStart = 46,
                                    ColumnEnd = 55,
                                    IsKey = false
                                },
                                new Column()
                                {
                                    ColumnName = "date2",
                                    ColumnStart = 56,
                                    ColumnEnd = 65,
                                    IsKey = false
                                },
                                new Column()
                                {
                                    ColumnName = "data2",
                                    ColumnStart = 66,
                                    ColumnEnd = 79,
                                    IsKey = false,
                                    TrimWhitespace = true
                                },
                                new Column()
                                {
                                    ColumnName = "data3",
                                    ColumnStart = 293,
                                    ColumnEnd = 317,
                                    IsKey = false,
                                    TrimWhitespace = false
                                }
                            }
                        }
                    }
            };
        }

        private ConnectRequest GetConnectSettings(string cleanupAction = null, string delimiter = ",",
            string filter = null, bool multiRoot = false, bool fromConfigFile = false, bool fromGlobalConfigFile = false, bool withName = false)
        {
            var settings = GetSettings(cleanupAction, delimiter, filter, multiRoot);

            if (withName)
            {
                foreach (var rootPath in settings.RootPaths)
                {
                    rootPath.Name = TestTableName;
                    rootPath.Columns = null;
                    rootPath.ColumnsConfigurationFile = ColumnsConfigFilePath;
                }
            }

            if (fromGlobalConfigFile)
            {
                settings.GlobalColumnsConfigurationFile = GlobalColumnsConfigFilePath;
            }

            if (fromConfigFile)
            {
                foreach (var rootPath in settings.RootPaths)
                {
                    rootPath.Columns = null;
                    rootPath.ColumnsConfigurationFile = ColumnsConfigFilePath;
                }
            }

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
        public async Task ConnectConfigurationFileTest()
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

            var request = GetConnectSettings(null, ",", null, false, true);

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
        public async Task ConnectGlobalConfigurationFileTest()
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

            var request = GetConnectSettings(null, ",", null, false, false, true);

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
        public async Task ConnectGlobalConfigurationFileWithNameTest()
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

            var request = GetConnectSettings(null, ",", null, false, false, true, true);

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
        public async Task ConnectGlobalAndLocalConfigurationFileTest()
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

            var request = GetConnectSettings(null, ",", null, false, true, true);

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
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectory]", schema.Id);
            Assert.Equal("ReadDirectory", schema.Name);
            Assert.Equal($"", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(6, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("id", property.Id);
            Assert.Equal("id", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.True(property.IsKey);
            Assert.False(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task DiscoverSchemasMultipleAllTest()
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

            var connectRequest = GetConnectSettings(null, ",", null, true);

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            // act
            client.Connect(connectRequest);
            client.DiscoverSchemas(request);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Equal(2, response.Schemas.Count);

            var schema = response.Schemas[0];
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectory]", schema.Id);
            Assert.Equal("ReadDirectory", schema.Name);
            Assert.Equal($"", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(6, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("id", property.Id);
            Assert.Equal("id", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.True(property.IsKey);
            Assert.False(property.IsNullable);

            var schema2 = response.Schemas[1];
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectoryDifferent]", schema2.Id);
            Assert.Equal("ReadDirectoryDifferent", schema2.Name);
            Assert.Equal($"", schema2.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema2.Count.Kind);
            // Assert.Equal(1000, schema2.Count.Value);
            Assert.Equal(10, schema2.Sample.Count);
            Assert.Equal(3, schema2.Properties.Count);

            var property2 = schema2.Properties[0];
            Assert.Equal("id", property2.Id);
            Assert.Equal("id", property2.Name);
            Assert.Equal("", property2.Description);
            Assert.Equal(PropertyType.String, property2.Type);
            Assert.True(property2.IsKey);
            Assert.False(property2.IsNullable);

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
                ToRefresh = {GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]")},
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
            Assert.Equal(1000, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("46904477493454029959SPNUZMVK63614033839X", record["id"]);
            Assert.Equal("40", record["data1"]);
            Assert.Equal("2019-09-04", record["date1"]);
            Assert.Equal("2019-09-04", record["date2"]);
            Assert.Equal("1827C30681", record["data2"]);
            Assert.Equal("    000000000000000293.44", record["data3"]);

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