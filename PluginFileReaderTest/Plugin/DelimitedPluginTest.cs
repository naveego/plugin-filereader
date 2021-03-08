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
    public class DelimitedPluginTest
    {
        private const string BasePath = "../../../MockData/DelimitedData";
        private const string ReadPath = "../../../MockData/ReadDirectory";
        private const string ReadDifferentPath = "../../../MockData/ReadDirectoryDifferent";
        private const string ArchivePath = "../../../MockData/ArchiveDirectory";
        private const string ReplicationPath = "../../../MockData/ReplicationDirectory";
        private const string TargetWriteFile = "target.csv";
        private const string GoldenReplicationFile = "golden.csv";
        private const string VersionReplicationFile = "version.csv";
        private const string DefaultCleanupAction = "none";
        private const string DefaultFilter = "*.csv";
        private const string DelimitedMode = "Delimited";

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

        private Settings GetSettings(string cleanupAction = null, string delimiter = ",",
            string filter = null, bool multiRoot = false, bool sftp = false)
        {
            return new Settings
            {
                FtpHostname = "",
                FtpPort = 21,
                FtpUsername = "",
                FtpPassword = "",
                RootPaths = multiRoot
                    ? new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = sftp ? "/CSV" : ReadPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = DelimitedMode,
                            FileReadMode = sftp ? "FTP" : "Local",
                            Delimiter = delimiter,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = sftp ? "/CSV/Archive" : ArchivePath
                        },
                        new RootPathObject
                        {
                            RootPath = sftp ? "/CSV" : ReadDifferentPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = DelimitedMode,
                            FileReadMode = sftp ? "FTP" : "Local",
                            Delimiter = delimiter,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = sftp ? "/CSV/Archive" : ArchivePath
                        }
                    }
                    : new List<RootPathObject>
                    {
                        new RootPathObject
                        {
                            RootPath = sftp ? "/CSV" : ReadPath,
                            Filter = filter ?? DefaultFilter,
                            Delimiter = delimiter,
                            Mode = DelimitedMode,
                            FileReadMode = sftp ? "FTP" : "Local",
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = sftp ? "/CSV/Archive" : ArchivePath
                        }
                    }
            };
        }

        private ConnectRequest GetConnectSettings(string cleanupAction = null, string delimiter = ",",
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

            var settings = GetSettings(cleanupAction, delimiter, filter, multiRoot, sftp);

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

        private Schema GetReplicationSchema()
        {
            return new Schema
            {
                Id = "test",
                Name = "test",
                Query = "",
                Properties =
                {
                    new Property
                    {
                        Id = "Id",
                        Name = "Id",
                        Type = PropertyType.Integer,
                        IsKey = true
                    },
                    new Property
                    {
                        Id = "Name",
                        Name = "Name",
                        Type = PropertyType.String
                    }
                }
            };
        }

        [Fact]
        public async Task ConnectSessionTest()
        {
            // setup
            PrepareTestEnvironment();
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
            PrepareTestEnvironment();
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
        public async Task ConnectEmptyTest()
        {
            // setup
            PrepareTestEnvironment();
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
        public async Task DiscoverSchemasAllTest()
        {
            // setup
            PrepareTestEnvironment();
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
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(6, schema.Properties.Count);
            Assert.Equal("", schema.PublisherMetaJson);

            var property = schema.Properties[0];
            Assert.Equal("id", property.Id);
            Assert.Equal("id", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasMultipleRequestsTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);
            var client2 = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings();

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };

            // act
            client.Connect(connectRequest);
            client2.Connect(connectRequest);
            var responseAsync = client.DiscoverSchemasAsync(request);
            var response2 = client2.DiscoverSchemasAsync(request);

            // assert
            var response = await responseAsync;
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            var schema = response.Schemas[0];
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectory]", schema.Id);
            Assert.Equal("ReadDirectory", schema.Name);
            Assert.Equal($"", schema.Query);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(6, schema.Properties.Count);
            Assert.Equal("", schema.PublisherMetaJson);

            var property = schema.Properties[0];
            Assert.Equal("id", property.Id);
            Assert.Equal("id", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasAllEmptyTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(null, ",", null, false, true);

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
            Assert.Empty(response.Schemas);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasMultipleAllTest()
        {
            // setup
            PrepareTestEnvironment();
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
                SampleSize = 0
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
            Assert.Equal(5, schema.Sample.Count);
            Assert.Equal(6, schema.Properties.Count);
            Assert.Equal("", schema.PublisherMetaJson);

            var property = schema.Properties[0];
            Assert.Equal("id", property.Id);
            Assert.Equal("id", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            var schema2 = response.Schemas[1];
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectoryDifferent]", schema2.Id);
            Assert.Equal("ReadDirectoryDifferent", schema2.Name);
            Assert.Equal($"", schema2.Query);
            Assert.Equal(5, schema2.Sample.Count);
            Assert.Equal(4, schema2.Properties.Count);
            Assert.Equal("", schema.PublisherMetaJson);

            var property2 = schema2.Properties[0];
            Assert.Equal("id", property2.Id);
            Assert.Equal("id", property2.Name);
            Assert.Equal("", property2.Description);
            Assert.Equal(PropertyType.String, property2.Type);
            Assert.False(property2.IsKey);
            Assert.True(property2.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasAllDelimiterTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("none", "|", "*.psv");

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

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasAllDelimiterTabTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("none", "\\t", "*.tsv");

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

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasAllDuplicateColumnTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("none", ",", "*.txt");

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

            var property = response.Schemas[0].Properties[0];
            Assert.Equal("id", property.Id);
            Assert.Equal("id", property.Name);

            var propertyDupe = response.Schemas[0].Properties[3];
            Assert.Equal("id_DUPLICATE_4", propertyDupe.Id);
            Assert.Equal("id_DUPLICATE_4", propertyDupe.Name);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }


        [Fact]
        public async Task DiscoverSchemasRefreshTest()
        {
            // setup
            PrepareTestEnvironment();
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
                ToRefresh =
                {
                    new Schema
                    {
                        Id = $"[{Constants.SchemaName}].[ReadDirectory]",
                        Name = "ReadDirectory",
                    }
                },
            };

            // act
            client.Connect(connectRequest);
            client.DiscoverSchemas(discoverAllRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);
            
            var schema = response.Schemas[0];
            Assert.Equal($"[{Constants.SchemaName}].[ReadDirectory]", schema.Id);
            Assert.Equal("ReadDirectory", schema.Name);
            Assert.Equal($"", schema.Query);
            Assert.Equal(5, schema.Sample.Count);
            Assert.Equal(6, schema.Properties.Count);
            Assert.Equal("", schema.PublisherMetaJson);

            var property = schema.Properties[0];
            Assert.Equal("id", property.Id);
            Assert.Equal("id", property.Name);
            Assert.Equal("", property.Description);
            Assert.Equal(PropertyType.String, property.Type);
            Assert.False(property.IsKey);
            Assert.True(property.IsNullable);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task NoDiscoverSchemasRefreshTest()
        {
            // setup
            PrepareTestEnvironment();
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

            /*var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
                SampleSize = 10
            };*/

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]")},
            };

            // act
            client.Connect(connectRequest);
            // client.DiscoverSchemas(discoverAllRequest);
            var response = client.DiscoverSchemas(request);

            // assert
            Assert.IsType<DiscoverSchemasResponse>(response);
            Assert.Single(response.Schemas);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task DiscoverSchemasRefreshQueryBadSyntaxTest()
        {
            // setup
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
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                SampleSize = 10,
                ToRefresh = {GetTestSchema("bad syntax")}
            };

            // act
            client.Connect(connectRequest);

            try
            {
                var response = client.DiscoverSchemas(request);
            }
            catch (Exception e)
            {
                // assert
                Assert.IsType<RpcException>(e);
                Assert.Contains("syntax error", e.Message);
            }

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamQueryBasedSchemaTest()
        {
            // setup
            PrepareTestEnvironment();
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

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var request = new ReadRequest()
            {
                Schema = GetTestSchema($@"select a.id, a.first_name, a.last_name, b.car_make
from ReadDirectory as a
inner join ReadDirectoryDifferent as b
on a.id = b.id"),
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
            Assert.Equal(1000, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamInvalidQueryBasedSchemaTest()
        {
            // setup
            PrepareTestEnvironment();
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

            var request = new ReadRequest()
            {
                Schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[NOPE]"),
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

            Assert.Empty(records);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamDirectoryBasedSchemaTest()
        {
            // setup
            PrepareTestEnvironment();
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
            Assert.Equal(2000, records.Count);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamEmptyDirectoryBasedSchemaTest()
        {
            // setup
            PrepareTestEnvironment(false, false, true);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var settings = GetSettings();
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                RootPath = settings.RootPaths.First()
            });
            schema.Properties.Add(new Property
            {
                Id = "testProp"
            });

            var connectRequest = GetConnectSettings();

            var discoverRefreshRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh = {schema}
            };

            var request = new ReadRequest()
            {
                Schema = schema,
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            // act
            client.Connect(connectRequest);
            client.DiscoverSchemas(discoverRefreshRequest);
            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Empty(records);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamLimitTest()
        {
            // setup
            PrepareTestEnvironment();
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

        [Fact]
        public async Task ReadStreamFtpTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(Constants.CleanupActionNone, ",", "*.csv", false, false, true);

            var configureRequest = new ConfigureRequest
            {
                TemporaryDirectory = "../../../Temp",
                PermanentDirectory = "../../../Perm",
                LogDirectory = "../../../Logs",
                DataVersions = new DataVersions(),
                LogLevel = LogLevel.Debug
            };

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            // act
            client.Configure(configureRequest);
            client.Connect(connectRequest);
            var discoverResponse = client.DiscoverSchemas(discoverAllRequest);

            var request = new ReadRequest()
            {
                Schema = discoverResponse.Schemas[0],
                Limit = 10,
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

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

        [Fact]
        public async Task ReadStreamCleanUpArchiveFullTest()
        {
            // setup
            PrepareTestEnvironment(false, true);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("Archive");

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings("Archive");
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                RootPath = settings.RootPaths.First()
            });

            var request = new ReadRequest()
            {
                Schema = schema,
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

            var readFiles = Directory.GetFiles(ReadPath, DefaultFilter);
            var archiveFiles = Directory.GetFiles(ArchivePath, DefaultFilter);

            var secondResponse = client.ReadStream(request);
            var secondResponseStream = secondResponse.ResponseStream;
            var secondRecords = new List<Record>();
            while (await secondResponseStream.MoveNext())
            {
                secondRecords.Add(secondResponseStream.Current);
            }

            // assert
            Assert.Equal(2000, records.Count);
            Assert.Empty(secondRecords);
            Assert.Empty(readFiles);
            Assert.Equal(5, archiveFiles.Length);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamCleanUpArchiveEmptyTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("Archive");

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings("Archive");
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                RootPath = settings.RootPaths.First()
            });

            var request = new ReadRequest()
            {
                Schema = schema,
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

            var readFiles = Directory.GetFiles(ReadPath, DefaultFilter);
            var archiveFiles = Directory.GetFiles(ArchivePath, DefaultFilter);

            var secondResponse = client.ReadStream(request);
            var secondResponseStream = secondResponse.ResponseStream;
            var secondRecords = new List<Record>();
            while (await secondResponseStream.MoveNext())
            {
                secondRecords.Add(secondResponseStream.Current);
            }

            // assert
            Assert.Equal(2000, records.Count);
            Assert.Empty(secondRecords);
            Assert.Empty(readFiles);
            Assert.Equal(2, archiveFiles.Length);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamCleanUpDeleteTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginFileReader.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("Delete");

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var settings = GetSettings("Delete");
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                RootPath = settings.RootPaths.First()
            });

            var request = new ReadRequest()
            {
                Schema = schema,
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

            var readFiles = Directory.GetFiles(ReadPath, DefaultFilter);
            var archiveFiles = Directory.GetFiles(ArchivePath, DefaultFilter);

            var secondResponse = client.ReadStream(request);
            var secondResponseStream = secondResponse.ResponseStream;
            var secondRecords = new List<Record>();
            while (await secondResponseStream.MoveNext())
            {
                secondRecords.Add(secondResponseStream.Current);
            }

            // assert
            Assert.Equal(2000, records.Count);
            Assert.Empty(secondRecords);
            Assert.Empty(readFiles);
            Assert.Empty(archiveFiles);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task PrepareWriteReplicationTest()
        {
            // setup
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

            var request = new PrepareWriteRequest()
            {
                Schema = GetReplicationSchema(),
                CommitSlaSeconds = 1,
                Replication = new ReplicationWriteRequest
                {
                    SettingsJson = JsonConvert.SerializeObject(new ConfigureReplicationFormData
                    {
                        GoldenRecordFileDirectory = ReplicationPath,
                        GoldenRecordFileName = GoldenReplicationFile,
                        VersionRecordFileDirectory = ReplicationPath,
                        VersionRecordFileName = VersionReplicationFile,
                        Delimiter = "\\t",
                        IncludeHeader = true
                    })
                },
                DataVersions = new DataVersions
                {
                    JobId = "jobUnitTest",
                    ShapeId = "shapeUnitTest",
                    JobDataVersion = 1,
                    ShapeDataVersion = 2
                }
            };

            // act
            client.Connect(connectRequest);
            var response = client.PrepareWrite(request);

            // assert
            Assert.IsType<PrepareWriteResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReplicationWriteTest()
        {
            // setup
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

            var prepareWriteRequest = new PrepareWriteRequest()
            {
                Schema = GetReplicationSchema(),
                CommitSlaSeconds = 1,
                Replication = new ReplicationWriteRequest
                {
                    SettingsJson = JsonConvert.SerializeObject(new ConfigureReplicationFormData
                    {
                        GoldenRecordFileDirectory = ReplicationPath,
                        GoldenRecordFileName = GoldenReplicationFile,
                        VersionRecordFileDirectory = ReplicationPath,
                        VersionRecordFileName = VersionReplicationFile,
                        Delimiter = ",",
                        IncludeHeader = true
                    })
                },
                DataVersions = new DataVersions
                {
                    JobId = "jobUnitTest",
                    ShapeId = "shapeUnitTest",
                    JobDataVersion = 1,
                    ShapeDataVersion = 2
                }
            };

            var records = new List<Record>()
            {
                {
                    new Record
                    {
                        Action = Record.Types.Action.Upsert,
                        CorrelationId = "test",
                        RecordId = "record1",
                        DataJson = "{\"Id\":1,\"Name\":\"Test Company\"}",
                        Versions =
                        {
                            new RecordVersion
                            {
                                RecordId = "version1",
                                DataJson = "{\"Id\":1,\"Name\":\"Test Company\"}",
                            }
                        }
                    }
                }
            };

            var recordAcks = new List<RecordAck>();

            // act
            client.Connect(connectRequest);
            client.PrepareWrite(prepareWriteRequest);

            using (var call = client.WriteStream())
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var ack = call.ResponseStream.Current;
                        recordAcks.Add(ack);
                    }
                });

                foreach (Record record in records)
                {
                    await call.RequestStream.WriteAsync(record);
                }

                await call.RequestStream.CompleteAsync();
                await responseReaderTask;
            }

            // assert
            Assert.Single(recordAcks);
            Assert.Equal("", recordAcks[0].Error);
            Assert.Equal("test", recordAcks[0].CorrelationId);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReplicationWriteFtpTest()
        {
            // setup
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

            var configureRequest = new ConfigureRequest
            {
                TemporaryDirectory = "../../../Temp",
                PermanentDirectory = "../../../Perm",
                LogDirectory = "../../../Logs",
                DataVersions = new DataVersions(),
                LogLevel = LogLevel.Debug
            };

            var prepareWriteRequest = new PrepareWriteRequest()
            {
                Schema = GetReplicationSchema(),
                CommitSlaSeconds = 1,
                Replication = new ReplicationWriteRequest
                {
                    SettingsJson = JsonConvert.SerializeObject(new ConfigureReplicationFormData
                    {
                        FileWriteMode = Constants.FileModeFtp,
                        GoldenRecordFileDirectory = "/Repl",
                        GoldenRecordFileName = GoldenReplicationFile,
                        VersionRecordFileDirectory = "/Repl",
                        VersionRecordFileName = VersionReplicationFile,
                        Delimiter = ",",
                        IncludeHeader = true
                    })
                },
                DataVersions = new DataVersions
                {
                    JobId = "jobUnitTest",
                    ShapeId = "shapeUnitTest",
                    JobDataVersion = 1,
                    ShapeDataVersion = 2
                }
            };

            var records = new List<Record>()
            {
                {
                    new Record
                    {
                        Action = Record.Types.Action.Upsert,
                        CorrelationId = "test",
                        RecordId = "record1",
                        DataJson = "{\"Id\":1,\"Name\":\"Test Company\"}",
                        Versions =
                        {
                            new RecordVersion
                            {
                                RecordId = "version1",
                                DataJson = "{\"Id\":1,\"Name\":\"Test Company\"}",
                            }
                        }
                    }
                }
            };

            var recordAcks = new List<RecordAck>();

            // act
            client.Configure(configureRequest);
            client.Connect(connectRequest);
            client.PrepareWrite(prepareWriteRequest);

            using (var call = client.WriteStream())
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var ack = call.ResponseStream.Current;
                        recordAcks.Add(ack);
                    }
                });

                foreach (Record record in records)
                {
                    await call.RequestStream.WriteAsync(record);
                }

                await call.RequestStream.CompleteAsync();
                await responseReaderTask;
            }

            // assert
            Assert.Single(recordAcks);
            Assert.Equal("", recordAcks[0].Error);
            Assert.Equal("test", recordAcks[0].CorrelationId);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ConfigureWriteTest()
        {
            // setup
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

            var request = new ConfigureWriteRequest
            {
                Form = new ConfigurationFormRequest
                {
                    DataJson = JsonConvert.SerializeObject(new ConfigureWriteFormData
                    {
                        TargetFileName = TargetWriteFile,
                        TargetFileDirectory = ReplicationPath,
                        Delimiter = ",",
                        CustomHeader = "custom header",
                        NullValue = "null",
                        IncludeHeader = true,
                        QuoteWrap = false,
                        Columns = new List<WriteColumn>
                        {
                            new WriteColumn
                            {
                                Name = "C1",
                                DefaultValue = ""
                            },
                            new WriteColumn
                            {
                                Name = "C2",
                                DefaultValue = "default"
                            },
                        }
                    })
                }
            };

            // act
            client.Connect(connectRequest);
            var response = client.ConfigureWrite(request);

            // assert
            Assert.IsType<ConfigureWriteResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ConfigureWriteFtpTest()
        {
            // setup
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

            var configureRequest = new ConfigureRequest
            {
                TemporaryDirectory = "../../../Temp",
                PermanentDirectory = "../../../Perm",
                LogDirectory = "../../../Logs",
                DataVersions = new DataVersions(),
                LogLevel = LogLevel.Debug
            };

            var request = new ConfigureWriteRequest
            {
                Form = new ConfigurationFormRequest
                {
                    DataJson = JsonConvert.SerializeObject(new ConfigureWriteFormData
                    {
                        FileWriteMode = Constants.FileModeFtp,
                        TargetFileName = "output.csv",
                        TargetFileDirectory = "/Output",
                        Delimiter = ",",
                        CustomHeader = "custom header",
                        NullValue = "null",
                        IncludeHeader = true,
                        QuoteWrap = false,
                        Columns = new List<WriteColumn>
                        {
                            new WriteColumn
                            {
                                Name = "C1",
                                DefaultValue = ""
                            },
                            new WriteColumn
                            {
                                Name = "C2",
                                DefaultValue = "default"
                            },
                        }
                    })
                }
            };

            // act
            client.Configure(configureRequest);
            client.Connect(connectRequest);
            var response = client.ConfigureWrite(request);

            // assert
            Assert.IsType<ConfigureWriteResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ConfigureReplicationTest()
        {
            // setup
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


            var request = new ConfigureReplicationRequest
            {
                Form = new ConfigurationFormRequest
                {
                    DataJson = JsonConvert.SerializeObject(
                        new ConfigureReplicationFormData
                        {
                            GoldenRecordFileDirectory = ReplicationPath,
                            GoldenRecordFileName = GoldenReplicationFile,
                            VersionRecordFileDirectory = ReplicationPath,
                            VersionRecordFileName = VersionReplicationFile,
                            Delimiter = ",",
                            CustomHeader = "custom header",
                            NullValue = "null",
                            IncludeHeader = true,
                            QuoteWrap = false,
                        }
                    )
                }
            };

            // act
            client.Connect(connectRequest);
            var response = client.ConfigureReplication(request);

            // assert
            Assert.IsType<ConfigureReplicationResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ConfigureReplicationFtpTest()
        {
            // setup
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

            var configureRequest = new ConfigureRequest
            {
                TemporaryDirectory = "../../../Temp",
                PermanentDirectory = "../../../Perm",
                LogDirectory = "../../../Logs",
                DataVersions = new DataVersions(),
                LogLevel = LogLevel.Debug
            };

            var request = new ConfigureReplicationRequest
            {
                Form = new ConfigurationFormRequest
                {
                    DataJson = JsonConvert.SerializeObject(
                        new ConfigureReplicationFormData
                        {
                            FileWriteMode = Constants.FileModeFtp,
                            GoldenRecordFileDirectory = "/Repl",
                            GoldenRecordFileName = GoldenReplicationFile,
                            VersionRecordFileDirectory = "/Repl",
                            VersionRecordFileName = VersionReplicationFile,
                            Delimiter = ",",
                            CustomHeader = "custom header",
                            NullValue = "null",
                            IncludeHeader = true,
                            QuoteWrap = false,
                        }
                    )
                }
            };

            // act
            client.Configure(configureRequest);
            client.Connect(connectRequest);
            var response = client.ConfigureReplication(request);

            // assert
            Assert.IsType<ConfigureReplicationResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task PrepareWriteWritebackTest()
        {
            // setup
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

            var configRequest = new ConfigureWriteRequest
            {
                Form = new ConfigurationFormRequest
                {
                    DataJson = JsonConvert.SerializeObject(new ConfigureWriteFormData
                    {
                        TargetFileName = TargetWriteFile,
                        TargetFileDirectory = ReplicationPath,
                        Delimiter = ",",
                        CustomHeader = "custom header",
                        NullValue = "null",
                        IncludeHeader = true,
                        QuoteWrap = false,
                        Columns = new List<WriteColumn>
                        {
                            new WriteColumn
                            {
                                Name = "C1",
                                DefaultValue = ""
                            },
                            new WriteColumn
                            {
                                Name = "C2",
                                DefaultValue = "default"
                            },
                        }
                    })
                }
            };

            // act
            client.Connect(connectRequest);
            var configResponse = client.ConfigureWrite(configRequest);

            var request = new PrepareWriteRequest()
            {
                Schema = configResponse.Schema,
                CommitSlaSeconds = 1,
                Replication = null,
                DataVersions = new DataVersions
                {
                    JobId = "jobUnitTest",
                    ShapeId = "shapeUnitTest",
                    JobDataVersion = 1,
                    ShapeDataVersion = 2
                }
            };

            var response = client.PrepareWrite(request);

            // assert
            Assert.IsType<PrepareWriteResponse>(response);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task WritebackTest()
        {
            // setup
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

            var configRequest = new ConfigureWriteRequest
            {
                Form = new ConfigurationFormRequest
                {
                    DataJson = JsonConvert.SerializeObject(new ConfigureWriteFormData
                    {
                        TargetFileName = TargetWriteFile,
                        TargetFileDirectory = ReplicationPath,
                        Delimiter = ",",
                        CustomHeader = "custom header",
                        NullValue = "null",
                        IncludeHeader = true,
                        QuoteWrap = false,
                        Columns = new List<WriteColumn>
                        {
                            new WriteColumn
                            {
                                Name = "Id",
                                DefaultValue = ""
                            },
                            new WriteColumn
                            {
                                Name = "Name",
                                DefaultValue = "default"
                            },
                        }
                    })
                }
            };

            // act
            client.Connect(connectRequest);

            var configResponse = client.ConfigureWrite(configRequest);

            var prepareWriteRequest = new PrepareWriteRequest()
            {
                Schema = configResponse.Schema,
                CommitSlaSeconds = 1,
                Replication = null,
                DataVersions = new DataVersions
                {
                    JobId = "jobUnitTest_write",
                    ShapeId = "shapeUnitTest",
                    JobDataVersion = 1,
                    ShapeDataVersion = 2
                }
            };

            client.PrepareWrite(prepareWriteRequest);

            var records = new List<Record>()
            {
                {
                    new Record
                    {
                        Action = Record.Types.Action.Upsert,
                        CorrelationId = "test",
                        RecordId = "record1",
                        DataJson = "{\"Id\":1}",
                        Versions =
                        {
                            new RecordVersion
                            {
                                RecordId = "version1",
                                DataJson = "{\"Id\":1}",
                            }
                        }
                    }
                }
            };

            var recordAcks = new List<RecordAck>();

            using (var call = client.WriteStream())
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var ack = call.ResponseStream.Current;
                        recordAcks.Add(ack);
                    }
                });

                foreach (Record record in records)
                {
                    await call.RequestStream.WriteAsync(record);
                }

                await call.RequestStream.CompleteAsync();
                await responseReaderTask;
            }

            // assert
            Assert.Single(recordAcks);
            Assert.Equal("", recordAcks[0].Error);
            Assert.Equal("test", recordAcks[0].CorrelationId);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task WritebackFtpTest()
        {
            // setup
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

            var configureRequest = new ConfigureRequest
            {
                TemporaryDirectory = "../../../Temp",
                PermanentDirectory = "../../../Perm",
                LogDirectory = "../../../Logs",
                DataVersions = new DataVersions(),
                LogLevel = LogLevel.Debug
            };

            var configRequest = new ConfigureWriteRequest
            {
                Form = new ConfigurationFormRequest
                {
                    DataJson = JsonConvert.SerializeObject(new ConfigureWriteFormData
                    {
                        FileWriteMode = Constants.FileModeFtp,
                        TargetFileName = TargetWriteFile,
                        TargetFileDirectory = "/Output",
                        Delimiter = ",",
                        CustomHeader = "custom header",
                        NullValue = "null",
                        IncludeHeader = true,
                        QuoteWrap = false,
                        Columns = new List<WriteColumn>
                        {
                            new WriteColumn
                            {
                                Name = "Id",
                                DefaultValue = ""
                            },
                            new WriteColumn
                            {
                                Name = "Name",
                                DefaultValue = "default"
                            },
                        }
                    })
                }
            };

            // act
            client.Configure(configureRequest);
            client.Connect(connectRequest);

            var configResponse = client.ConfigureWrite(configRequest);

            var prepareWriteRequest = new PrepareWriteRequest()
            {
                Schema = configResponse.Schema,
                CommitSlaSeconds = 1,
                Replication = null,
                DataVersions = new DataVersions
                {
                    JobId = "jobUnitTest_write",
                    ShapeId = "shapeUnitTest",
                    JobDataVersion = 1,
                    ShapeDataVersion = 2
                }
            };

            client.PrepareWrite(prepareWriteRequest);

            var records = new List<Record>()
            {
                {
                    new Record
                    {
                        Action = Record.Types.Action.Upsert,
                        CorrelationId = "test",
                        RecordId = "record1",
                        DataJson = "{\"Id\":1}",
                        Versions =
                        {
                            new RecordVersion
                            {
                                RecordId = "version1",
                                DataJson = "{\"Id\":1}",
                            }
                        }
                    }
                }
            };

            var recordAcks = new List<RecordAck>();

            using (var call = client.WriteStream())
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var ack = call.ResponseStream.Current;
                        recordAcks.Add(ack);
                    }
                });

                foreach (Record record in records)
                {
                    await call.RequestStream.WriteAsync(record);
                }

                await call.RequestStream.CompleteAsync();
                await responseReaderTask;
            }

            // assert
            Assert.Single(recordAcks);
            Assert.Equal("", recordAcks[0].Error);
            Assert.Equal("test", recordAcks[0].CorrelationId);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}