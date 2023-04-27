using System.Collections.Generic;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Discover
{
    public static partial class Discover
    {
        public static Schema GetFileInfoSchema(ServerCallContext context,
            List<RootPathConnectObject> rootPathConnects, int sampleSize = 5)
        {
            Logger.Info("GetFileInfoSchema: Start");

            var schema = new Schema
            {
                Id = "AU_FileInformation",
                Name = "AU_FileInformation",
                DataFlowDirection = Schema.Types.DataFlowDirection.Read,
                Properties =
                {
                    new Property
                    {
                        Id = "RootPathName",
                        Name = "Root Path Name",
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
                        Name = "File Type",
                        IsKey = false,
                        IsNullable = false,
                        Type = PropertyType.String
                    },
                    new Property
                    {
                        Id = "FileSize",
                        Name = "File Size",
                        IsKey = false,
                        IsNullable = false,
                        Type = PropertyType.String
                    }
                }
            };

            for (int i = 0; i < rootPathConnects.Count; i++)
            {
                // TODO: Discover all files and their matches in root paths
            }

            Logger.Info($"Schemas returned: {schema}");
            return schema;
        }
    }
}