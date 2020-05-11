using System;
using System.Threading.Tasks;
using PluginFileReader.API.Factory;
using PluginFileReader.DataContracts;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static DateTime LastWriteTime { get; set; }

        public static void WriteToDisk(IImportExportFile goldenImportExport,
            IImportExportFile versionImportExport, ConfigureReplicationFormData config)
        {
            // check if 5 seconds have passed since last write to disk
            if ((DateTime.Now - LastWriteTime).TotalSeconds >= 5)
            {
                // write out to disk
                goldenImportExport.ExportTable(config.GetGoldenFilePath());
                versionImportExport.ExportTable(config.GetVersionFilePath());
            }
        }
    }
}