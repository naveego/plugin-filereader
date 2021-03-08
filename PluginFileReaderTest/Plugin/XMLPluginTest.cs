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
            string filter = null, bool multiRoot = false, bool sftp = false)
        {
            return new Settings
            {
                FtpHostname = "",
                FtpPort = 2222,
                FtpUsername = "",
                FtpPassword = "",
                RootPaths = multiRoot
                    ? new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = sftp ? "/sftp/xmltest" : ReadPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = XMLMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            FileReadMode = sftp ? "SFTP" : "Local",
                            ModeSettings = new ModeSettings
                            {
                                XMLSettings = new XmlSettings
                                {
                                    XmlKeys = new List<XmlKey>
                                    {
                                        new XmlKey
                                        {
                                            ElementId = "CONSUMER",
                                            AttributeId = "ssn",
                                        }
                                    },
                                    XsdFilePathAndName =
                                        sftp
                                            ? "/sftp/xmltest/xsd/VL_CREDITREPORT.xsd"
                                            : "../../../MockData/XMLData/VL_CREDITREPORT.xsd"
                                }
                            }
                        },
                        new RootPathObject
                        {
                            RootPath = sftp ? "/sftp/xmltest" : ReadDifferentPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = XMLMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            FileReadMode = sftp ? "SFTP" : "Local",
                            ModeSettings = new ModeSettings
                            {
                                XMLSettings = new XmlSettings
                                {
                                    XmlKeys = new List<XmlKey>
                                    {
                                        new XmlKey
                                        {
                                            ElementId = "CONSUMER",
                                            AttributeId = "ssn",
                                        }
                                    },
                                    XsdFilePathAndName =
                                        sftp
                                            ? "/sftp/xmltest/xsd/VL_CREDITREPORT.xsd"
                                            : "../../../MockData/XMLData/VL_CREDITREPORT.xsd"
                                }
                            }
                        }
                    }
                    : new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = sftp ? "/sftp/xmltest" : ReadPath,
                            Filter = filter ?? DefaultFilter,
                            SkipLines = skipLines,
                            Mode = XMLMode,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = ArchivePath,
                            FileReadMode = sftp ? "SFTP" : "Local",
                            ModeSettings = new ModeSettings
                            {
                                XMLSettings = new XmlSettings
                                {
                                    XmlKeys = new List<XmlKey>
                                    {
                                        new XmlKey
                                        {
                                            ElementId = "CONSUMER",
                                            AttributeId = "ssn",
                                        }
                                    },
                                    XsdFilePathAndName =
                                        sftp
                                            ? "/sftp/xmltest/xsd/VL_CREDITREPORT.xsd"
                                            : "../../../MockData/XMLData/VL_CREDITREPORT.xsd"
                                }
                            }
                        }
                    }
            };
        }

        private ConnectRequest GetConnectSettings(string cleanupAction = null, int skipLines = 0,
            string filter = null, bool multiRoot = false, bool empty = false, bool sftp = false)
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
            Assert.Equal(19, response.Schemas.Count);

            var schema = response.Schemas[0];
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectory_HEADER]", schema.Id);
            Assert.Equal("ReadDirectory_HEADER", schema.Name);
            Assert.Equal($"", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(1, schema.Sample.Count);
            Assert.Equal(19, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("report_date", property.Id);
            Assert.Equal("report_date", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasAllSftpTest()
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

            var connectRequest = GetConnectSettings(null, 0, null, false, false, true);

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
            Assert.Equal(19, response.Schemas.Count);

            var schema = response.Schemas[0];
            Assert.Equal($"[{Constants.SchemaName}].[xmltest_HEADER]", schema.Id);
            Assert.Equal("xmltest_HEADER", schema.Name);
            Assert.Equal($"SELECT * FROM [{Constants.SchemaName}].[xmltest_HEADER]", schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(1, schema.Sample.Count);
            Assert.Equal(19, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("report_date", property.Id);
            Assert.Equal("report_date", property.Name);
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

            var query = $@"SELECT
h.firstname,
m.risk_score,
r.factor_code,
r.factor_text
FROM ReadDirectory_CONSUMER h
LEFT OUTER JOIN ReadDirectory_RISK_MODEL m
ON h.GLOBAL_KEY = m.GLOBAL_KEY
LEFT OUTER JOIN ReadDirectory_RISK_FACTOR r
ON m.GLOBAL_KEY = r.GLOBAL_KEY";

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema(query)},
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal($"test", schema.Id);
            Assert.Equal("test", schema.Name);
            Assert.Equal(query, schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(3, schema.Sample.Count);
            Assert.Equal(4, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("firstname", property.Id);
            Assert.Equal("firstname", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

               [Fact]
        public async Task DiscoverSchemasRefreshSftpTest()
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

            var connectRequest = GetConnectSettings(null, 0, null, false, false, true);
            
            var query = $@"SELECT
h.firstname,
m.risk_score,
r.factor_code,
r.factor_text
FROM xmltest_CONSUMER h
LEFT OUTER JOIN xmltest_RISK_MODEL m
ON h.GLOBAL_KEY = m.GLOBAL_KEY
LEFT OUTER JOIN xmltest_RISK_FACTOR r
ON m.GLOBAL_KEY = r.GLOBAL_KEY";

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema(query)},
            };

            // act
            client.Connect(connectRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal($"test", schema.Id);
            Assert.Equal("test", schema.Name);
            Assert.Equal(query, schema.Query);
            // Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            // Assert.Equal(1000, schema.Count.Value);
            Assert.Equal(4, schema.Sample.Count);
            Assert.Equal(4, schema.Properties.Count);

            var property = schema.Properties[0];
            Assert.Equal("firstname", property.Id);
            Assert.Equal("firstname", property.Name);
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

            var connectRequest = GetConnectSettings();
            
            var query = $@"SELECT
h.ssn,
h.firstname,
m.risk_score,
r.factor_code,
r.factor_text
FROM ReadDirectory_CONSUMER h
LEFT OUTER JOIN ReadDirectory_RISK_MODEL m
ON h.GLOBAL_KEY = m.GLOBAL_KEY
LEFT OUTER JOIN ReadDirectory_RISK_FACTOR r
ON m.GLOBAL_KEY = r.GLOBAL_KEY";

            var discoverRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema(query)},
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
            Assert.Equal( 28, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("5164", record["ssn"]);
            Assert.Equal("string", record["firstname"]);

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

            var connectRequest = GetConnectSettings(null, 0, null, false, false, true);
            
            var query = $@"SELECT
h.ssn,
h.firstname,
m.risk_score,
r.factor_code,
r.factor_text
FROM xmltest_CONSUMER h
LEFT OUTER JOIN xmltest_RISK_MODEL m
ON h.GLOBAL_KEY = m.GLOBAL_KEY
LEFT OUTER JOIN xmltest_RISK_FACTOR r
ON m.GLOBAL_KEY = r.GLOBAL_KEY";

            var discoverRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema(query)},
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
            Assert.Equal( 4, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("6075", record["ssn"]);
            Assert.Equal("string", record["firstname"]);

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
            
            var query = $@"SELECT
h.ssn,
h.firstname,
m.risk_score,
r.factor_code,
r.factor_text
FROM ReadDirectory_CONSUMER h
LEFT OUTER JOIN ReadDirectory_RISK_MODEL m
ON h.GLOBAL_KEY = m.GLOBAL_KEY
LEFT OUTER JOIN ReadDirectory_RISK_FACTOR r
ON m.GLOBAL_KEY = r.GLOBAL_KEY";

            var discoverRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema(query)},
            };
            
            var request = new ReadRequest()
            {
                Limit = 1,
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
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
            Assert.Equal( 1, records.Count);

            var record = JsonConvert.DeserializeObject<Dictionary<string, object>>(records[0].DataJson);
            Assert.Equal("5164", record["ssn"]);
            Assert.Equal("string", record["firstname"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}