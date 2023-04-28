using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf.Collections;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;

namespace PluginFileReader.Helper
{
    public class FileInfoData
    {
        public string RootPathName { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileSize { get; set; }

        public static Record CreateFromLocalPath(string pathToFile)
        {
            // null if the path is a directory instead
            if (Directory.Exists(pathToFile)) return null;

            var fileInfo = new FileInfo(pathToFile);
            if (!fileInfo.Exists) return null;

            var extensionData = fileInfo.Extension;
            if (!string.IsNullOrWhiteSpace(fileInfo.Extension) && extensionData.Length > 1)
                extensionData = $"{fileInfo.Extension.Substring(1).ToUpper()} file";
            else extensionData = null;

            return new Record
            {
                Action = Record.Types.Action.Upsert,
                DataJson = GenerateRecordData(new FileInfoData
                {
                    RootPathName = fileInfo.Directory.FullName,
                    FileName = fileInfo.Name,
                    FileType = extensionData,
                    FileSize = fileInfo.Length.ToString()
                })
            };
        }

        public static readonly RepeatedField<Property> FileInfoProperties = new RepeatedField<Property>
        {
            new Property
            {
                Id = "RootPathName",
                Name = "Root Path",
                IsKey = true,
                IsNullable = false,
                Type = PropertyType.String
            },
            new Property
            {
                Id = "FileName",
                Name = "File Name",
                IsKey = true,
                IsNullable = false,
                Type = PropertyType.String
            },
            new Property
            {
                Id = "FileType",
                Name = "Type",
                IsKey = false,
                IsNullable = false,
                Type = PropertyType.String
            },
            new Property
            {
                Id = "FileSize",
                Name = "Size",
                IsKey = false,
                IsNullable = false,
                Type = PropertyType.String
            }
        };

        private static string GenerateRecordData(FileInfoData data)
        {
            var resultData = new Dictionary<string, object>();
            foreach (var property in data.GetType().GetProperties())
            {
                resultData.TryAdd(property.Name, property.GetValue(data));
            }

            return JsonConvert.SerializeObject(resultData);
        }
    }
}