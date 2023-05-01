using Google.Protobuf.Collections;
using Naveego.Sdk.Plugins;

namespace PluginFileReader.Helper
{
    public static class FileInfoData
    {
        public const string FileInfoSchemaId = "AU_FileInformation";

        public static Schema GetFileInfoSchema()
        {
            return new Schema
            {
                Id = FileInfoSchemaId,
                Name = FileInfoSchemaId,
                DataFlowDirection = Schema.Types.DataFlowDirection.Read,
                Properties =
                {
                    FileInfoData.FileInfoProperties
                }
            };
        }

        public static bool IsFileInfoSchema(Schema schema)
        {
            return schema.Id.Trim(' ') == FileInfoData.FileInfoSchemaId;
        }

        public static readonly RepeatedField<Property> FileInfoProperties = new RepeatedField<Property>
        {
            new Property
            {
                Id = "RootPath",
                Name = "Root Path",
                IsKey = true,
                IsNullable = false,
                Type = PropertyType.String,
                TypeAtSource = $"VARCHAR({int.MaxValue})"
            },
            new Property
            {
                Id = "FileName",
                Name = "File Name",
                IsKey = true,
                IsNullable = false,
                Type = PropertyType.String,
                TypeAtSource = $"VARCHAR({int.MaxValue})"
            },
            new Property
            {
                Id = "FileExtension",
                Name = "File Extension",
                IsKey = false,
                IsNullable = false,
                Type = PropertyType.String,
                TypeAtSource = $"VARCHAR({int.MaxValue})"
            },
            new Property
            {
                Id = "FileSize",
                Name = "File Size",
                IsKey = false,
                IsNullable = false,
                Type = PropertyType.String,
                TypeAtSource = $"VARCHAR({int.MaxValue})"
            }
        };
    }
}