

using Naveego.Sdk.Plugins;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        public static PropertyType GetWritebackType(string type)
        {
            switch (type)
            {
                case "string":
                    return PropertyType.String;
                case "bool":
                    return PropertyType.Bool;
                case "int":
                    return PropertyType.Integer;
                case "float":
                    return PropertyType.Float;
                case "decimal":
                    return PropertyType.Decimal;
                default:
                    return PropertyType.String;
            }
        }
    }
}