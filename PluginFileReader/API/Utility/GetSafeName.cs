namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static string GetSafeName(string unsafeName)
        {
            return $"[{unsafeName}]";
        }
    }
}