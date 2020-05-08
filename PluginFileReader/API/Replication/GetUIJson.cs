using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        public static string GetUIJson()
        {
            var uiJsonObj = new Dictionary<string, object>
            {
                {"ui:order", new []
                {
                    "GoldenRecordFileDirectory",
                    "GoldenRecordFileName",
                    "VersionRecordFileDirectory",
                    "VersionRecordFileName",
                    "IncludeHeader",
                    "Delimiter",
                }}
            };

            return JsonConvert.SerializeObject(uiJsonObj);
        }
    }
}