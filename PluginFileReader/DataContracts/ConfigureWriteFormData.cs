using System.Collections.Generic;
using System.IO;

namespace PluginFileReader.DataContracts
{
    public class ConfigureWriteFormData
    {
        public string TargetFileDirectory { get; set; }
        public string TargetFileName { get; set; }
        public bool IncludeHeader { get; set; }
        public bool QuoteWrap { get; set; }
        public string Delimiter { get; set; }
        public string NullValue { get; set; }
        public string CustomHeader { get; set; }
        public List<WriteColumn> Columns { get; set; }
        
        public char GetDelimiter()
        {
            switch (Delimiter)
            {
                case "\\t":
                    return '\t';
                default:
                    return char.Parse(Delimiter);
            }
        }

        public string GetTargetTableName()
        {
            return Path.GetFileNameWithoutExtension(TargetFileName);
        }

        public string GetTargetFilePath()
        {
            return Path.Join(TargetFileDirectory, TargetFileName);
        }

        public ConfigureReplicationFormData GetReplicationFormData()
        {
            return new ConfigureReplicationFormData
            {
                Delimiter = Delimiter,
                CustomHeader = CustomHeader,
                IncludeHeader = IncludeHeader,
                NullValue = NullValue,
                QuoteWrap = QuoteWrap,
                GoldenRecordFileDirectory = TargetFileDirectory,
                GoldenRecordFileName = TargetFileName,
                VersionRecordFileDirectory = "",
                VersionRecordFileName = ""
            };
        }
    }

    public class WriteColumn
    {
        public string Name { get; set; }
        public string DefaultValue { get; set; }
    }
}