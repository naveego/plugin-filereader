using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        public static string GetSchemaJson()
        {
            var schemaJsonObj = new Dictionary<string, object>
            {
                {"type", "object"},
                {"properties", new Dictionary<string, object>
                {
                    {"GoldenRecordFileDirectory", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Golden Record File Directory"},
                        {"description", "Path to the folder to place the Golden Record file."},
                    }},
                    {"GoldenRecordFileName", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Golden Record File Name"},
                        {"description", "Name of Golden Record file"},
                    }},
                    {"VersionRecordFileDirectory", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Version Record File Directory"},
                        {"description", "Path to the folder to place the Version Record file."},
                    }},
                    {"VersionRecordFileName", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Golden Record File Name"},
                        {"description", "Name of Version Record file."},
                    }},
                    {"IncludeHeader", new Dictionary<string, object>
                    {
                        {"type", "boolean"},
                        {"title", "Include Header"},
                        {"description", "Include a header row in the output files?"},
                        {"default", false},
                    }},
                    {"Delimiter", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"title", "Delimiter"},
                        {"description", "Delimiter to use in the output files."},
                        {"default", ","},
                        {"minLength", 1},
                        {"maxLength", 1},
                    }},
                }},
                {"required", new []
                {
                    "GoldenRecordFileDirectory",
                    "GoldenRecordFileName",
                    "VersionRecordFileDirectory",
                    "VersionRecordFileName",
                    "IncludeHeader",
                    "Delimiter",
                }}
            };
            
            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}