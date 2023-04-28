using System.Collections.Generic;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using PluginFileReader.Helper;
using PluginFileReader.DataContracts;
using System.IO;

namespace PluginFileReader.API.Discover
{
    public static partial class Discover
    {
        public const string FileInfoSchemaId = "AU_FileInformation";

        public static Schema GetFileInfoSchema()
        {
            var schema = new Schema
            {
                Id = FileInfoSchemaId,
                Name = FileInfoSchemaId,
                DataFlowDirection = Schema.Types.DataFlowDirection.Read,
                Properties =
                {
                    FileInfoData.FileInfoProperties
                }
            };

            Logger.Info($"File Info Schema returned: {schema}");
            return schema;
        }
    }
}