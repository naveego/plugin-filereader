using System.IO;
using PluginFileReader.API.Utility;
using PluginFileReader.Helper;

namespace PluginFileReader.DataContracts
{
    public class ConfigureReplicationFormData
    {
        public string GoldenRecordFileDirectory { get; set; }
        public string GoldenRecordFileName { get; set; }
        public string VersionRecordFileDirectory { get; set; }
        public string VersionRecordFileName { get; set; }
        public string FileWriteMode { get; set; }
        public bool IncludeHeader { get; set; }
        public bool QuoteWrap { get; set; }
        public string Delimiter { get; set; }
        public string NullValue { get; set; }
        public string CustomHeader { get; set; }
        
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

        public string GetGoldenTableName()
        {
            if (string.IsNullOrWhiteSpace(GoldenRecordFileName))
            {
                return Constants.DefaultGoldenTable;
            }
            return Path.GetFileNameWithoutExtension(GoldenRecordFileName);
        }

        public string GetLocalGoldenFilePath()
        {
            switch (FileWriteMode)
            {
                case Constants.FileModeFtp:
                case Constants.FileModeSftp:
                    return Path.Join(Utility.TempDirectory, GoldenRecordFileDirectory, GoldenRecordFileName);
                default:
                    return Path.Join(GoldenRecordFileDirectory, GoldenRecordFileName);
            }
        }
        
        public string GetRemoteGoldenFilePath()
        {
            return Path.Join(GoldenRecordFileDirectory, GoldenRecordFileName);
        }
        
        public string GetLocalGoldenDirectory()
        {
            switch (FileWriteMode)
            {
                case Constants.FileModeFtp:
                case Constants.FileModeSftp:
                    return Path.Join(Utility.TempDirectory, GoldenRecordFileDirectory);
                default:
                    return Path.Join(GoldenRecordFileDirectory);
            }
        }
        
        public string GetRemoteGoldenDirectory()
        {
            return Path.Join(GoldenRecordFileDirectory);
        }

        public string GetVersionTableName()
        {
            if (string.IsNullOrWhiteSpace(VersionRecordFileName))
            {
                return Constants.DefaultVersionTable;
            }
            return Path.GetFileNameWithoutExtension(VersionRecordFileName);
        }

        public string GetLocalVersionFilePath()
        {
            switch (FileWriteMode)
            {
                case Constants.FileModeFtp:
                case Constants.FileModeSftp:
                    return Path.Join(Utility.TempDirectory, VersionRecordFileDirectory, VersionRecordFileName);
                default:
                    return Path.Join(VersionRecordFileDirectory, VersionRecordFileName);
            }
        }
        
        public string GetRemoteVersionFilePath()
        {
            return Path.Join(VersionRecordFileDirectory, VersionRecordFileName);
        }
        
        public string GetLocalVersionDirectory()
        {
            switch (FileWriteMode)
            {
                case Constants.FileModeFtp:
                case Constants.FileModeSftp:
                    return Path.Join(Utility.TempDirectory, VersionRecordFileDirectory);
                default:
                    return Path.Join(VersionRecordFileDirectory);
            }
        }
        
        public string GetRemoteVersionDirectory()
        {
            return Path.Join(VersionRecordFileDirectory);
        }
        
        public void ConvertLegacyConfiguration()
        {
            if (string.IsNullOrWhiteSpace(FileWriteMode))
            {
                FileWriteMode = Constants.FileModeLocal;
            }
        }
    }
}