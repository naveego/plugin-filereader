using System.Collections.Generic;
using PluginFileReader.DataContracts;

namespace PluginFileReader.API.Utility
{
    public static class Constants
    {
        public static string SchemaName = "dbo";
        
        public static string DbFolder = "db";
        public static string DbFile = "plugin-filereader.db";
        public static string DiscoverDbPrefix = "discover";
        
        public static string ImportMetaDataTableName = "naveego_import_metadata";
        public static string ImportMetaDataPathColumn = "path";
        public static string ImportMetaDataLastModifiedDate = "last_modified";
        
        public static string DefaultGoldenTable = "NaveegoDefaultGolden";
        public static string DefaultVersionTable = "NaveegoDefaultVersion";
        
        public static string ReplicationRecordId = "NaveegoReplicationRecordId";
        public static string ReplicationVersionIds = "NaveegoVersionIds";
        public static string ReplicationLineNumber = "NaveegoLineNumber";
        public static string ReplicationVersionRecordId = "NaveegoReplicationVersionRecordId";
        
        public static string ReplicationMetaDataTableName = "NaveegoReplicationMetaData";
        public static string ReplicationMetaDataJobId = "NaveegoJobId";
        public static string ReplicationMetaDataRequest = "Request";
        public static string ReplicationMetaDataReplicatedShapeId = "NaveegoShapeId";
        public static string ReplicationMetaDataReplicatedShapeName = "NaveegoShapeName";
        public static string ReplicationMetaDataTimestamp = "Timestamp";

        // Mode options
        public const string ModeDelimited = "Delimited";
        public const string ModeFixedWidth = "Fixed Width Columns";
        public const string ModeExcel = "Excel";
        public const string ModeAS400 = "AS400";
        public const string ModeXML = "XML";

        // File Mode options
        public const string FileModeLocal = "Local";
        public const string FileModeFtp = "FTP";
        public const string FileModeSftp = "SFTP";

        // Cleanup Action options
        public const string CleanupActionNone = "None";
        public const string CleanupActionArchive = "Archive";
        public const string CleanupActionDelete = "Delete";
        
        public static List<ReplicationColumn> ReplicationMetaDataColumns = new List<ReplicationColumn>
        {
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataJobId,
                DataType = $"varchar({int.MaxValue})",
                PrimaryKey = true
            },
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataRequest,
                PrimaryKey = false,
                DataType = $"varchar({int.MaxValue})"
            },
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataReplicatedShapeId,
                DataType = $"varchar({int.MaxValue})",
                PrimaryKey = false
            },
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataReplicatedShapeName,
                DataType = $"varchar({int.MaxValue})",
                PrimaryKey = false
            },
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataTimestamp,
                DataType = $"varchar({int.MaxValue})",
                PrimaryKey = false
            }
        };
    }
}