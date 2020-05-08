using System.IO;
using PluginFileReader.Helper;

namespace PluginFileReader.DataContracts
{
    public class ConfigureReplicationFormData
    {
        public string GoldenRecordFileDirectory { get; set; }
        public string GoldenRecordFileName { get; set; }
        public string VersionRecordFileDirectory { get; set; }
        public string VersionRecordFileName { get; set; }
        public bool IncludeHeader { get; set; }
        public char Delimiter { get; set; }

        public string GetGoldenTableName()
        {
            return Path.GetFileNameWithoutExtension(GoldenRecordFileName);
        }

        public RootPathObject GetGoldenRootPath()
        {
            return new RootPathObject
            {
                Delimiter = Delimiter,
                HasHeader = IncludeHeader,
                RootPath = GetGoldenFilePath(),
            };
        }

        public string GetGoldenFilePath()
        {
            return Path.Join(GoldenRecordFileDirectory, GoldenRecordFileName);
        }

        public string GetVersionTableName()
        {
            return Path.GetFileNameWithoutExtension(VersionRecordFileName);
        }
        
        public RootPathObject GetVersionRootPath()
        {
            return new RootPathObject
            {
                Delimiter = Delimiter,
                HasHeader = IncludeHeader,
                RootPath = GetVersionFilePath(),
            };
        }

        public string GetVersionFilePath()
        {
            return Path.Join(VersionRecordFileDirectory, VersionRecordFileName);
        }
    }
}