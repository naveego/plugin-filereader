namespace PluginCSV.API.Utility
{
    public static class Constants
    {
        public static string SchemaName = "dbo";
        
        public static string DbFolder = "db";
        public static string DbFile = "plugincsv.db";
        public static string DiscoverDbPrefix = "discover";
        
        public static string ImportMetaDataTableName = "naveego_import_metadata";
        public static string ImportMetaDataPathColumn = "path";
        public static string ImportMetaDataLastModifiedDate = "last_modified";
    }
}