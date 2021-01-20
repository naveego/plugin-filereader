using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        public static string GetSchemaJson()
        {
            var schemaJsonObj = new Dictionary<string, object>
            {
                {"type", "object"},
                {"properties", new Dictionary<string, object>
                {
                    {"TargetFileDirectory", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Target File Directory"},
                        {"description", "Path to the folder to place the target file."},
                    }},
                    {"TargetFileName", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Target File Name"},
                        {"description", "Name of target file"},
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
                    {"Columns", new Dictionary<string, object>
                    {
                        {"type", "array"},
                        {"title", "Columns"},
                        {"description", "Columns to writeback."},
                        {"items", new Dictionary<string, object>
                        {
                            {"type", "object"},
                            {"properties", new Dictionary<string, object>
                            {
                                {"Name", new Dictionary<string, object>
                                {
                                    {"type", "string"},
                                    {"title", "Name"},
                                    {"description", "Name of the column."},
                                    {"default", ""},
                                }},
                                {"DefaultValue", new Dictionary<string, object>
                                {
                                    {"type", "string"},
                                    {"title", "Default Value"},
                                    {"description", "Default value of the column."},
                                    {"default", ""},
                                }},
                            }},
                            {"required", new []
                            {
                                "Name"
                            }}
                        }}
                    }},
                }},
                {"required", new []
                {
                    "TargetFileDirectory",
                    "TargetFileName",
                    "IncludeHeader",
                    "Delimiter",
                }}
            };
            
            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}