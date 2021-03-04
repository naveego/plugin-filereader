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

        public static void WriteToDisk(IImportExportFile targetImportExport, ConfigureWriteFormData config, Settings settings, bool forceWrite = false)
        {
            // check if 5 seconds have passed since last write to disk
            if (forceWrite || (DateTime.Now - LastWriteTime).TotalSeconds >= 5 && PendingWrites)
            {
                // write out to disk
                targetImportExport.ExportTable(config.GetLocalTargetFilePath());
                PendingWrites = false;
                
                // write to Remote
                if (config.FileWriteMode != Constants.FileModeLocal)
                {
                    var localFileName = config.GetLocalTargetFilePath();
                    var remoteFileName = config.GetRemoteTargetFilePath();
                    
                    switch (config.FileWriteMode)
                    {
                        case Constants.FileModeFtp:
                            using (var client = Utility.Utility.GetFtpClient())
                            {
                                try
                                {
                                    var status = client.UploadFile(localFileName, remoteFileName);
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
                            using (var client = Utility.Utility.GetSftpClient())
                            {
                                try
                                {
                                    var fileStream = Utility.Utility.GetStream(localFileName, config.FileWriteMode);
                                    client.UploadFile(fileStream.Stream, remoteFileName);
                                    fileStream.Close();
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