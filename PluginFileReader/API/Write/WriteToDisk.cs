using System;
using System.IO;
using FluentFTP;
using PluginFileReader.API.Factory;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        private static DateTime LastWriteTime { get; set; }
        private static bool PendingWrites { get; set; }

        public static void WriteToDisk(IImportExportFile targetImportExport, ConfigureWriteFormData config, Settings settings)
        {
            // check if 5 seconds have passed since last write to disk
            if ((DateTime.Now - LastWriteTime).TotalSeconds >= 5 && PendingWrites)
            {
                // write out to disk
                targetImportExport.ExportTable(config.GetTargetFilePath());
                PendingWrites = false;
                
                // write to Remote
                if (config.FileWriteMode != Constants.FileModeLocal)
                {
                    var remoteFileName = Path.Join(config.TargetFileDirectory, config.TargetFileName);
                    
                    switch (config.FileWriteMode)
                    {
                        case Constants.FileModeFtp:
                            using (var client = Utility.Utility.GetFtpClient(settings))
                            {
                                try
                                {
                                    var status = client.UploadFile(config.GetTargetFilePath(), remoteFileName);
                                    if (status == FtpStatus.Failed)
                                    {
                                        throw new Exception($"Could not write file to remote {remoteFileName}");
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
                                    using (var fileStream = Utility.Utility.GetFileStream(config.GetTargetFilePath()))
                                    {
                                        client.UploadFile(fileStream, remoteFileName);
                                    }
                                }
                                catch
                                {
                                    throw new Exception($"Could not write file to remote {remoteFileName}");
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
        }

        public static void PurgeWriteFile()
        {
            // set triggers for async file write
            LastWriteTime = DateTime.Now;
            PendingWrites = true;
        }
    }
}