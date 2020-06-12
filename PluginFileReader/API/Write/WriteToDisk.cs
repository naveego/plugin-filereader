using System;
using PluginFileReader.API.Factory;
using PluginFileReader.DataContracts;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        private static DateTime LastWriteTime { get; set; }
        private static bool PendingWrites { get; set; }

        public static void WriteToDisk(IImportExportFile targetImportExport, ConfigureWriteFormData config)
        {
            // check if 5 seconds have passed since last write to disk
            if ((DateTime.Now - LastWriteTime).TotalSeconds >= 5 && PendingWrites)
            {
                // write out to disk
                targetImportExport.ExportTable(config.GetTargetFilePath());
                PendingWrites = false;
            }
        }

        public static void PurgeWriteFile()
        {
            // set triggers for async file write
            LastWriteTime = DateTime.Now;
            PendingWrites = true;
        }
    }
}