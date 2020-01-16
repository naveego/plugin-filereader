using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Newtonsoft.Json;
using PluginCSV.API.Utility;
using PluginCSV.DataContracts;
using PluginCSV.Helper;
using Pub;
using Xunit;
using Record = Pub.Record;

namespace PluginCSVTest.Plugin
{
    public class DelimitiedPluginTest
    {
        private readonly string DatabasePath = $"{Path.Join(Constants.DbFolder, Constants.DbFile)}";
        private const string BasePath = "../../../MockData/Data";
        private const string ReadPath = "../../../MockData/ReadDirectory";
        private const string ReadDifferentPath = "../../../MockData/ReadDirectoryDifferent";
        private const string ArchivePath = "../../../MockData/ArchiveDirectory";
        private const string DefaultCleanupAction = "none";
        private readonly List<string> DefaultFilters = new List<string> {"*.csv"};

        private void PrepareTestEnvironment(bool configureInvalid)
        {
            File.Delete(DatabasePath);

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

        private ConnectRequest GetConnectSettings(string cleanupAction = null, char delimiter = ',',
            List<string> filters = null, bool multiRoot = false)
        {
            var settings = new Settings
            {
                RootPaths = multiRoot ? new List<string> {ReadPath, ReadDifferentPath} : new List<string> {ReadPath},
                Filters = filters ?? DefaultFilters,
                Delimiter = delimiter,
                HasHeader = true,
                CleanupAction = cleanupAction ?? DefaultCleanupAction,
                ArchivePath = ArchivePath
            };

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
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
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
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
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
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
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
            Assert.Equal($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]", schema.Query);
            Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            Assert.Equal(2000, schema.Count.Value);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(6, schema.Properties.Count);

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
        public async Task DiscoverSchemasMultipleAllTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(null, ',', null, true);

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
            Assert.Equal($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]", schema.Query);
            Assert.Equal(Count.Types.Kind.Exact, schema.Count.Kind);
            Assert.Equal(2000, schema.Count.Value);
            Assert.Equal(10, schema.Sample.Count);
            Assert.Equal(6, schema.Properties.Count);

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
            Assert.Equal($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectoryDifferent]", schema2.Query);
            Assert.Equal(Count.Types.Kind.Exact, schema2.Count.Kind);
            Assert.Equal(1000, schema2.Count.Value);
            Assert.Equal(10, schema2.Sample.Count);
            Assert.Equal(4, schema2.Properties.Count);

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
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("none", '|', new List<string> {"*.psv"});

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
        public async Task DiscoverSchemasRefreshTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
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
        public async Task ReadStreamQueryBasedSchemaTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings(null, ',', null, true);

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };

            var request = new ReadRequest()
            {
                Schema = GetTestSchema($@"select a.id, a.first_name, a.last_name, b.car_make
from ReadDirectory as a
inner join ReadDirectoryDifferent as b
on a.id = b.id")
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
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
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
                Schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[NOPE]")
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
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
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
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                Directory = ReadPath
            });

            var request = new ReadRequest()
            {
                Schema = schema
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
        public async Task ReadStreamCleanUpArchiveTest()
        {
            // setup
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("archive");

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                Directory = ReadPath
            });

            var request = new ReadRequest()
            {
                Schema = schema
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

            var readFiles = Directory.GetFiles(ReadPath, DefaultFilters.First());
            var archiveFiles = Directory.GetFiles(ArchivePath, DefaultFilters.First());

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
            PrepareTestEnvironment(false);
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("delete");

            var discoverAllRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
            };
            var schema = GetTestSchema($"SELECT * FROM [{Constants.SchemaName}].[ReadDirectory]");
            schema.PublisherMetaJson = JsonConvert.SerializeObject(new SchemaPublisherMetaJson
            {
                Directory = ReadPath
            });

            var request = new ReadRequest()
            {
                Schema = schema
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

            var readFiles = Directory.GetFiles(ReadPath, DefaultFilters.First());
            var archiveFiles = Directory.GetFiles(ArchivePath, DefaultFilters.First());

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
    }
}