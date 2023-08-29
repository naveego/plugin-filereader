using Google.Protobuf.Collections;
using Naveego.Sdk.Plugins;

namespace PluginFileReader.Helper
{
    public static class FileInfoData
    {
        public const string FileInfoSchemaId = "AU_FileInformation";

        public const string FileInfoSchemaDescription = "A .csv file which contains a list of all the file names " + 
             "present in the directory along with their root path and size.";
        public const string RootPathDescription = "The location of the file as per the system's file structure.";
        public const string FileNameDescription = "Name of the file as present in the root directory.";
        public const string FileSizeDescription = "Information on how many bytes of data the file contains.";

        public static Schema GetFileInfoSchema()
        {
            return new Schema
            {
                Id = FileInfoSchemaId,
                Name = FileInfoSchemaId,
                Description = FileInfoSchemaDescription,
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
                Description = RootPathDescription,
                IsKey = true,
                IsNullable = false,
                Type = PropertyType.String,
                TypeAtSource = $"VARCHAR({int.MaxValue})"
            },
            new Property
            {
                Id = "FileName",
                Name = "File Name",
                Description = FileNameDescription,
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
                IsNullable = true,
                Type = PropertyType.String,
                TypeAtSource = $"VARCHAR({int.MaxValue})"
            },
            new Property
            {
                Id = "FileSize",
                Name = "File Size",
                Description = FileSizeDescription,
                IsKey = false,
                IsNullable = false,
                Type = PropertyType.String,
                TypeAtSource = $"VARCHAR({int.MaxValue})"
            }
        };
    }
}