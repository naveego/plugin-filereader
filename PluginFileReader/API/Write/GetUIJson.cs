using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        public static string GetUIJson()
        {
            var uiJsonObj = new Dictionary<string, object>
            {
                {"ui:order", new []
                {
                    "TargetFileDirectory",
                    "TargetFileName",
                    "IncludeHeader",
                    "QuoteWrap",
                    "Delimiter",
                    "NullValue",
                    "CustomHeader",
                    "Columns"
                }},
                {"Columns", new Dictionary<string, object>
                {
                    {"items", new Dictionary<string,object>
                    {
                        {"ui:order", new []
                        {
                            "Name",
                            "DefaultValue",
                        }},
                    }}
                }}
            };
            
            return JsonConvert.SerializeObject(uiJsonObj);
        }
    }
}

// "ui": {
// "ui:order": ["GlobalColumnsConfigurationFile","RootPaths"],
// "RootPaths": {
//     "items": {
//         "ui:order": ["RootPath", "Filter", "Name", "CleanupAction", "ArchivePath", "Mode", "Delimiter", "HasHeader", "ColumnsConfigurationFile", "Columns"],
//         "Columns": {
//             "items": {
//                 "ui:order": ["ColumnName", "IsKey", "TrimWhitespace", "ColumnStart", "ColumnEnd"]
//             }
//         }
//     }
// }
// }