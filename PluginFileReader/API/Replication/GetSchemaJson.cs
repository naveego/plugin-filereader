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
                        {"title", "Version Record File Name"},
                        {"description", "Name of Version Record file."},
                    }},
                    {"FileWriteMode", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"title", "File Write Mode"},
                        {"description", "Mode to write target files."},
                        {"default", "Local"},
                        {"enum", new List<string>
                        {
                            "Local",
                            "FTP",
                            "SFTP"
                        }}
                    }},
                    {"IncludeHeader", new Dictionary<string, object>
                    {
                        {"type", "boolean"},
                        {"title", "Include Header"},
                        {"description", "Include a header row of the column names in the output files?"},
                        {"default", false},
                    }},
                    {"QuoteWrap", new Dictionary<string, object>
                    {
                        {"type", "boolean"},
                        {"title", "Quote Wrap"},
                        {"description", "Wrap all values in output file in double quotes?"},
                        {"default", false},
                    }},
                    {"Delimiter", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"title", "Delimiter"},
                        {"description", "Delimiter to use in the output files."},
                        {"default", ","},
                    }},
                    {"NullValue", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"title", "Null Value"},
                        {"description", "The value to write when the property value is null."},
                        {"default", "null"},
                    }},
                    {"CustomHeader", new Dictionary<string, object>
                    {
                        {"type", "string"},
                        {"title", "Custom Header"},
                        {"description", "Custom header to place on the first line of the output file."},
                        {"default", ""},
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