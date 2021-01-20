using System;
using System.IO;
using System.Threading.Tasks;
using FluentFTP;
using PluginFileReader.API.Factory;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        private static DateTime LastWriteTime { get; set; }
        private static bool PendingWrites { get; set; }

        public static void WriteToDisk(IImportExportFile goldenImportExport,
            IImportExportFile versionImportExport, ConfigureReplicationFormData config, Settings settings)
        {
            // check if 5 seconds have passed since last write to disk
            if ((DateTime.Now - LastWriteTime).TotalSeconds >= 5 && PendingWrites)
            {
                // write out to disk
                goldenImportExport.ExportTable(config.GetGoldenFilePath());
                versionImportExport.ExportTable(config.GetVersionFilePath());
                PendingWrites = false;
            }
            
            // write to Remote
                if (config.FileWriteMode != Constants.FileModeLocal)
                {
                    var remoteGoldenFileName = Path.Join(config.GoldenRecordFileDirectory, config.GoldenRecordFileName);
                    var remoteVersionFileName = Path.Join(config.VersionRecordFileDirectory, config.VersionRecordFileName);
                    
                    switch (config.FileWriteMode)
                    {
                        case Constants.FileModeFtp:
                            using (var client = Utility.Utility.GetFtpClient(settings))
                            {
                                try
                                {
                                    var status = client.UploadFile(config.GetGoldenFilePath(), remoteGoldenFileName);
                                    if (status == FtpStatus.Failed)
                                    {
                                        throw new Exception($"Could not write file to remote {remoteGoldenFileName}");
                                    }
                                    
                                    status = client.UploadFile(config.GetVersionFilePath(), remoteVersionFileName);
                                    if (status == FtpStatus.Failed)
                                    {
                                        throw new Exception($"Could not write file to remote {remoteVersionFileName}");
                                    }
                                }
                                finally
                                {
                                    client.Disconnect();
                                }
                            }

                            break;
                        case Constants.FileModeSftp:
                            using (var client = Utility.Utility.GetSftpClient(settings))
                            {
                                try
                                {
                                    using (var fileStream = Utility.Utility.GetFileStream(config.GetGoldenFilePath()))
                                    {
                                        client.UploadFile(fileStream, remoteGoldenFileName);
                                    }
                                    
                                    using (var fileStream = Utility.Utility.GetFileStream(config.GetVersionFilePath()))
                                    {
                                        client.UploadFile(fileStream, remoteVersionFileName);
                                    }
                                }
                                catch
                                {
                                    throw new Exception($"Could not write files to remote {remoteGoldenFileName} {remoteVersionFileName}");
                                }
                                finally
                                {
                                    client.Disconnect();
                                }
                            }

                            break;
                    }
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