using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Newtonsoft.Json;
using PluginCSV.Helper;
using Pub;
using Xunit;

namespace PluginCSVTest.Plugin
{
    public class PluginTest
    {
        private readonly string BasePath = "../../../MockData/Data";
        private readonly string ReadPath = "../../../MockData/ReadDirectory";
        private readonly string ArchivePath = "../../../MockData/ArchiveDirectory";

        private void PrepareTestEnvironment()
        {
            foreach (var filePath in Directory.GetFiles(ArchivePath))
            {
                File.Delete(filePath);
            }

            foreach (var filePath in Directory.GetFiles(BasePath))
            {
                var targetPath = $"{ReadPath}/{Path.GetFileName(filePath)}";
                File.Copy(filePath, targetPath, true);
            }
        }

        private ConnectRequest GetConnectSettings(string cleanupAction)
        {
            var settings = new Settings
            {
                RootPaths = new List<string> {ReadPath},
                Filters = new List<string> {"*.csv"},
                Delimiter = ',',
                HasHeader = true,
                CleanupAction = "none",
                ArchivePath = ArchivePath
            };

            if (cleanupAction != "none")
            {
                settings.CleanupAction = cleanupAction;
            }

            return new ConnectRequest
            {
                SettingsJson = JsonConvert.SerializeObject(settings),
                OauthConfiguration = new OAuthConfiguration(),
                OauthStateJson = ""
            };
        }
        
        [Fact]
        public async Task ConnectSessionTest()
        {
            // setup
            PrepareTestEnvironment();
            Server server = new Server
            {
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings("none");
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
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var request = GetConnectSettings("none");

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
                Services = {Publisher.BindService(new PluginCSV.Plugin.Plugin())},
                Ports = {new ServerPort("localhost", 0, ServerCredentials.Insecure)}
            };
            server.Start();

            var port = server.Ports.First().BoundPort;

            var channel = new Channel($"localhost:{port}", ChannelCredentials.Insecure);
            var client = new Publisher.PublisherClient(channel);

            var connectRequest = GetConnectSettings("none");

            var request = new DiscoverSchemasRequest
            {
                Mode = DiscoverSchemasRequest.Types.Mode.All,
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
    }
}