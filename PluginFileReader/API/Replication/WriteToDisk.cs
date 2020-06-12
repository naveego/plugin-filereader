using System;
using System.Threading.Tasks;
using PluginFileReader.API.Factory;
using PluginFileReader.DataContracts;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static DateTime LastWriteTime { get; set; }
        private static bool PendingWrites { get; set; }

        public static void WriteToDisk(IImportExportFile goldenImportExport,
            IImportExportFile versionImportExport, ConfigureReplicationFormData config)
        {
            // check if 5 seconds have passed since last write to disk
            if ((DateTime.Now - LastWriteTime).TotalSeconds >= 5 && PendingWrites)
            {
                // write out to disk
                goldenImportExport.ExportTable(config.GetGoldenFilePath());
                versionImportExport.ExportTable(config.GetVersionFilePath());
                PendingWrites = false;
            }
        }
        
        public static void PurgeReplicationFiles()
        {
            // set triggers for async file write
            LastWriteTime = DateTime.Now;
            PendingWrites = true;
        }
    }
}