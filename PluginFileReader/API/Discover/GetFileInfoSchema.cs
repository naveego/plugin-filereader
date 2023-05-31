using Newtonsoft.Json;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Discover
{
    public static partial class Discover
    {
        public static Schema GetFileInfoSchema()
        {
            var schema = FileInfoData.GetFileInfoSchema();
            Logger.Debug($"File Info Schema returned: {JsonConvert.SerializeObject(schema, Formatting.Indented)}");
            return schema;
        }
    }
}