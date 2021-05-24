using Naveego.Sdk.Plugins;

namespace PluginFileReader.API.Utility
{
    public partial class Utility
    {
        public static string GetDefaultQuery(Schema schema)
        {
            return $"SELECT * FROM {schema.Id}";
        }
    }
}