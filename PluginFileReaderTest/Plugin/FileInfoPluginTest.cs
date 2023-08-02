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
        private const string ReadSFTPPath = "";
        private const string ReadDifferentPath = "../../../MockData/ReadDirectoryDifferent";
        private const string ReadSFTPDifferentPath = "";
        private const string ArchivePath = "../../../MockData/ArchiveDirectory";
        private const string ArchiveSFTPPath = "";
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
                            RootPath = sftp ? ReadSFTPPath : ReadPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = FileCopyMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = sftp ? ArchiveSFTPPath : ArchivePath,
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
                            RootPath = sftp ? ReadSFTPDifferentPath : ReadDifferentPath,
                            Filter = filter ?? DefaultFilter,
                            Mode = FileCopyMode,
                            SkipLines = skipLines,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = sftp ? ArchiveSFTPPath : ArchivePath,
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
                            RootPath = sftp ? ReadSFTPPath : ReadPath,
                            Filter = filter ?? DefaultFilter,
                            SkipLines = skipLines,
                            Mode = FileCopyMode,
                            HasHeader = true,
                            CleanupAction = cleanupAction ?? DefaultCleanupAction,
                            ArchivePath = sftp ? ArchiveSFTPPath : ArchivePath,
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
            string filter = null, bool multiRoot = false, bool empty = false, bool sftp = false, int delayMS = 0, string oldRegexReplace = "",
            string newRegexReplace = "", bool noRootPaths = false)
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
            if (noRootPaths)
            {
                settings.RootPaths = new List<RootPathObject>();
            }

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
        public async Task ReadStreamEmptyTest()
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

            var connectRequest = GetConnectSettings(null, 0, "*.nvld", false, false, false, noRootPaths: true);

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
            Assert.Equal(0, records.Count);

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
            Assert.Equal(fileInfoSchema.Description, FileInfoData.FileInfoSchemaDescription);
            // assert description
            Assert.True(string.IsNullOrWhiteSpace(fileInfoSchema.Query));
            Assert.Equal(fileInfoSchema.DataFlowDirection, Schema.Types.DataFlowDirection.Read);
            Assert.Equal(fileInfoSchema.Properties.Count, FileInfoData.FileInfoProperties.Count);
     
            // assert properties and descriptions are correct
            var rootpathProp = fileInfoSchema.Properties.First(p => p.Id.Equals("RootPath"));
            Assert.Equal(rootpathProp.Id, "RootPath");
            Assert.Equal(rootpathProp.Description, "The location of the file as per the system's file structure.");

            var filenameProp = fileInfoSchema.Properties.First(p => p.Id.Equals("FileName"));
            Assert.Equal(filenameProp.Id, "FileName");
            Assert.Equal(filenameProp.Description, "Name of the file as present in the root directory.");

            var filesizeProp = fileInfoSchema.Properties.First(p => p.Id.Equals("FileSize"));
            Assert.Equal(filesizeProp.Id, "FileSize");
            Assert.Equal(filesizeProp.Description, "Information on how many bytes of data the file contains.");
 
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
            Assert.Equal(ReadPath, record["RootPath"]);
            Assert.Equal("VL_CREDITREPORT.xsd", record["FileName"]);
            Assert.Equal("XSD file", record["FileExtension"]);
            Assert.Equal("22.8KB", record["FileSize"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamRefreshTest()
        {
            // setup
            PrepareTestEnvironment(true);
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
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                ToRefresh =
                {
                    FileInfoData.GetFileInfoSchema()
                },
                SampleSize = 10
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

            Assert.Single(schemasResponse.Schemas);

            var fileInfoSchema = schemasResponse.Schemas.First();
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
            Assert.Equal(ReadPath, record["RootPath"]);
            Assert.Equal("<xsd-file>", record["FileName"]);
            Assert.Equal("XSD file", record["FileExtension"]);
            Assert.Equal("<xsd-filesize>", record["FileSize"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamRefreshTest2()
        {
            // setup
            PrepareTestEnvironment(true);
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

            // act - find AU_FileInformation schema, then read table
            client.Configure(configureRequest);
            client.Connect(connectRequest);
            var allSchemasResponse = client.DiscoverSchemas(
                new DiscoverSchemasRequest
                {
                    Mode = DiscoverSchemasRequest.Types.Mode.All
                }
            );

            var discoverRequest = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.Refresh,
                SampleSize = 10,
                ToRefresh =
                {
                    allSchemasResponse.Schemas.First(s => !FileInfoData.IsFileInfoSchema(s))
                }
            };
            var schemasResponse = client.DiscoverSchemas(discoverRequest);

            Assert.Single(schemasResponse.Schemas);

            var request = new ReadRequest()
            {
                DataVersions = new DataVersions
                {
                    JobId = "test"
                },
                JobId = "test",
            };

            request.Schema = schemasResponse.Schemas.First();

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

            var fileInfoSchema = schemasResponse.Schemas.First(s => FileInfoData.IsFileInfoSchema(s));
            Assert.Equal(fileInfoSchema.Id, FileInfoData.FileInfoSchemaId);
            Assert.Equal(fileInfoSchema.Name, FileInfoData.FileInfoSchemaId);
            Assert.True(string.IsNullOrWhiteSpace(fileInfoSchema.Query));
            Assert.Equal(fileInfoSchema.DataFlowDirection, Schema.Types.DataFlowDirection.Read);
            Assert.Equal(fileInfoSchema.Properties.Count, FileInfoData.FileInfoProperties.Count);

            request.Schema = schemasResponse.Schemas.First(s => FileInfoData.IsFileInfoSchema(s));

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
            Assert.Equal(ReadSFTPPath, record["RootPath"]);
            Assert.Equal("<xml-file>", record["FileName"]);
            Assert.Equal("XML file", record["FileExtension"]);
            Assert.Equal("<xml-filesize>", record["FileSize"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }

        [Fact]
        public async Task ReadStreamSftpMultirootTest()
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

            var connectRequest = GetConnectSettings(null, 0, "*", true, false, true);

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

            var fileInfoSchema = schemasResponse.Schemas.First(s => FileInfoData.IsFileInfoSchema(s));
            Assert.Equal(fileInfoSchema.Id, FileInfoData.FileInfoSchemaId);
            Assert.Equal(fileInfoSchema.Name, FileInfoData.FileInfoSchemaId);
            Assert.True(string.IsNullOrWhiteSpace(fileInfoSchema.Query));
            Assert.Equal(fileInfoSchema.DataFlowDirection, Schema.Types.DataFlowDirection.Read);
            Assert.Equal(fileInfoSchema.Properties.Count, FileInfoData.FileInfoProperties.Count);

            request.Schema = schemasResponse.Schemas.First(s => FileInfoData.IsFileInfoSchema(s));

            var response = client.ReadStream(request);
            var responseStream = response.ResponseStream;
            var records = new List<Record>();

            while (await responseStream.MoveNext())
            {
                records.Add(responseStream.Current);
            }

            // assert
            Assert.Equal(5, records.Count);

            var recordData = records.Select(r => JsonConvert.DeserializeObject<Dictionary<string, object>>(r.DataJson));

            var recordXML = recordData.First(d => (string)d["RootPath"] == ReadSFTPPath);
            Assert.Equal("<xml-file>", recordXML["FileName"]);
            Assert.Equal("XML file", recordXML["FileExtension"]);
            Assert.Equal("<xml-filesize>", recordXML["FileSize"]);

            var recordCSV = recordData.First(d => (string)d["RootPath"] == ReadSFTPDifferentPath);
            Assert.Equal("<csv-file>", recordCSV["FileName"]);
            Assert.Equal("CSV file", recordCSV["FileExtension"]);
            Assert.Equal("<csv-filesize>", recordCSV["FileSize"]);

            // cleanup
            await channel.ShutdownAsync();
            await server.ShutdownAsync();
        }
    }
}