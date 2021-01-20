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
                    "FileWriteMode",
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