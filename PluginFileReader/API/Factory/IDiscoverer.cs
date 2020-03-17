using System.Collections.Generic;
using Naveego.Sdk.Plugins;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Factory
{
    public interface IDiscoverer
    {
        public IEnumerable<Schema> DiscoverSchemas(IImportExportFactory factory, RootPathObject rootPath, List<string> paths, int sampleSize = 5);
    }
}