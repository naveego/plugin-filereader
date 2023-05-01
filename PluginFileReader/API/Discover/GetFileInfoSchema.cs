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
            Logger.Info($"File Info Schema returned: {schema}");
            return schema;
        }
    }
}