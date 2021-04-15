using System.Collections.Generic;
using System.Linq;
using Naveego.Sdk.Plugins;

namespace PluginFileReader.API.Discover
{
    public static partial class Discover
    {
        public static bool ShouldLoadAll(List<Schema> schemas)
        {
            return schemas.Any(s => s.Query.ToLower().Contains("count(*)"));
        }
    }
}