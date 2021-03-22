using System.Collections.Generic;
using Grpc.Core;
using Naveego.Sdk.Plugins;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Factory
{
    public interface IDiscoverer
    {
        public IEnumerable<Schema> DiscoverSchemas(ServerCallContext context, IImportExportFactory factory, RootPathObject rootPath, List<string> paths, int sampleSize = 5);
    }
}