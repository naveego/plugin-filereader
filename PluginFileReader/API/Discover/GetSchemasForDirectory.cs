using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Factory;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Discover
{
    public static partial class Discover
    {
        public static IEnumerable<Schema> GetSchemasForDirectory(IImportExportFactory factory, RootPathObject rootPath, List<string> paths,
            int sampleSize = 5)
        {
            var discoverer = factory.MakeDiscoverer();

            return discoverer.DiscoverSchemas(factory, rootPath, paths, sampleSize);
        }
    }
}