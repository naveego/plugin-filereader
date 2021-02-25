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
    public class XMLPluginTest
    {
        private const string BasePath = "../../../MockData/XMLData";
        private const string ReadPath = "../../../MockData/ReadDirectory";
        private const string ReadDifferentPath = "../../../MockData/ReadDirectoryDifferent";
        private const string ArchivePath = "../../../MockData/ArchiveDirectory";
        private const string ReplicationPath = "../../../MockData/ReplicationDirectory";
        private const string TargetWriteFile = "target.csv";
        private const string GoldenReplicationFile = "golden.csv";
        private const string VersionReplicationFile = "version.csv";
        private const string DefaultCleanupAction = "none";
        private const string DefaultFilter = "*.xml";
        private const string XMLMode = "XML";

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
                            Mode = XMLMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            ModeSettings = new ModeSettings{
                              XMLSettings = new XmlSettings
                              {
                                  XmlKeys = new List<XmlKey>
                                  {
                                      new XmlKey
                                      {
                                          ElementId = "HEADER",
                                          AttributeId = "customer_number",
                                      }
                                  }
                              }
                            }
                        },
                        new RootPathObject
                        {
                            RootPath = ReadDifferentPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = XMLMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            ModeSettings = new ModeSettings{
                                XMLSettings = new XmlSettings
                                {
                                    XmlKeys = new List<XmlKey>
                                    {
                                        new XmlKey
                                        {
                                            ElementId = "HEADER",
                                            AttributeId = "customer_number",
                                        }
                                    }
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
                            SkipLines = skipLines,
                            Mode = XMLMode,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            ModeSettings = new ModeSettings{
                                XMLSettings = new XmlSettings
                                {
                                    XmlKeys = new List<XmlKey>
                                    {
                                        new XmlKey
                                        {
                                            ElementId = "HEADER",
                                            AttributeId = "customer_number",
                                        }
                                    }
                                }
                            }
                        }
                    }
            };
        }

        private ConnectRequest GetConnectSettings(string cleanupAction = null, int skipLines = 0,
            string filter = null, bool multiRoot = false, bool empty = false)
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

            var settings = GetSettings(cleanupAction, skipLines, filter, multiRoot);

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
                Services = { Publisher.BindService(new PluginFileReader.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
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
                Services = { Publisher.BindService(new PluginFileReader.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
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
                Services = { Publisher.BindService(new PluginFileReader.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(null, 0, "array.xml");

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
            Assert.Equal($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(2, schema.Sample.Count);
            Assert.Equal(17, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("HEADER.@report_date", property.Id);
            Assert.Equal("HEADER.@report_date", property.Name);
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
                Services = { Publisher.BindService(new PluginFileReader.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(null, 0, "sample_vl_creditreport*.xml");

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = { GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory_HEADER]") },
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
            Assert.Equal(1, schema.Sample.Count);
            Assert.Equal(529, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("CREDITDATA.@xsi:noNamespaceSchemaLocation", property.Id);
            Assert.Equal("CREDITDATA.@xsi:noNamespaceSchemaLocation", property.Name);
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
                Services = { Publisher.BindService(new PluginFileReader.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(null, 0, "sample_vl_creditreport*.xml");

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings(null, 0, "sample_vl_creditreport*.xml");
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
            Assert.Equal(2, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("5164", record["CREDITDATA.CONSUMER.@ssn"]);
            Assert.Equal("string", record["CREDITDATA.CONSUMER.@firstname"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamXMLHeaderTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginFileReader.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(null, 0, "Sample-employee-XML-file.xml");

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings(null, 0, "Sample-employee-XML-file.xml");
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
            Assert.Equal(12, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            // Assert.Equal("bk101", record["@id"]);
            // Assert.Equal("Gambardella, Matthew", record["author"]);
            // Assert.Equal("Computer", record["genre"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
        
        [Fact]
        public async Task ReadStreamArrayTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = { Publisher.BindService(new PluginFileReader.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(null, 0, "array.xml");

            var schemaRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings(null, 0, "sample_vl_creditreport*.xml");
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
            Assert.Equal(2, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("5429", record["HEADER.@report_id"]);
            Assert.Equal("string", record["HEADER.@date_ordered"]);
            // Assert.Equal("1 ML", record["HCPCS Code Dosage"]);
            // Assert.Equal("115.892", record["Payment Limit"]);
            // Assert.Equal("", record["Vaccine AWP%"]);
            // Assert.Equal("", record["Vaccine Limit"]);

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
                Services = { Publisher.BindService(new PluginFileReader.Plugin.Plugin()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
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
            Assert.Equal(1, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}