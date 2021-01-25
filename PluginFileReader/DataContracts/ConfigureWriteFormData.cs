using System.Collections.Generic;
using System.IO;
using PluginFileReader.API.Utility;

namespace PluginFileReader.DataContracts
{
    public class ConfigureWriteFormData
    {
        public string TargetFileDirectory { get; set; }
        public string TargetFileName { get; set; }
        public string FileWriteMode { get; set; }
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

        public string GetLocalTargetDirectory()
        {
            switch (FileWriteMode)
            {
                case Constants.FileModeFtp:
                case Constants.FileModeSftp:
                    return Path.Join(Utility.TempDirectory, TargetFileDirectory);
                default:
                    return Path.Join(TargetFileDirectory);
            }
        }
        
        public string GetRemoteTargetDirectory()
        {
            return Path.Join(TargetFileDirectory);
        }

        public string GetLocalTargetFilePath()
        {
            switch (FileWriteMode)
            {
                case Constants.FileModeFtp:
                case Constants.FileModeSftp:
                    return Path.Join(Utility.TempDirectory, TargetFileDirectory, TargetFileName);
                default:
                    return Path.Join(TargetFileDirectory, TargetFileName);
            }
        }
        
        public string GetRemoteTargetFilePath()
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

        public void ConvertLegacyConfiguration()
        {
            if (string.IsNullOrWhiteSpace(FileWriteMode))
            {
                FileWriteMode = Constants.FileModeLocal;
            }
        }
    }

    public class WriteColumn
    {
        public string Name { get; set; }
        public string DefaultValue { get; set; }
    }
}