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
            IImportExportFile versionImportExport, ConfigureReplicationFormData config, Settings settings, bool forceWrite = false)
        {
            // check if 5 seconds have passed since last write to disk
            if (forceWrite || (DateTime.Now - LastWriteTime).TotalSeconds >= 5 && PendingWrites)
            {
                // write out to disk
                goldenImportExport.ExportTable(config.GetLocalGoldenFilePath());
                versionImportExport.ExportTable(config.GetLocalVersionFilePath());
                PendingWrites = false;

                // write to Remote
                if (config.FileWriteMode != Constants.FileModeLocal)
                {
                    var localGoldenFileName = config.GetLocalGoldenFilePath();
                    var remoteGoldenFileName = config.GetRemoteGoldenFilePath();
                    var localVersionFileName = config.GetLocalVersionFilePath();
                    var remoteVersionFileName = config.GetRemoteVersionFilePath();

                    switch (config.FileWriteMode)
                    {
                        case Constants.FileModeFtp:
                            using (var client = Utility.Utility.GetFtpClient())
                            {
                                try
                                {
                                    var status = client.UploadFile(localGoldenFileName, remoteGoldenFileName);
                                    if (status == FtpStatus.Failed)
                                    {
                                        throw new Exception($"Could not write file to remote {remoteGoldenFileName}");
                                    }

                                    status = client.UploadFile(localVersionFileName, remoteVersionFileName);
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
                            using (var client = Utility.Utility.GetSftpClient())
                            {
                                try
                                {
                                    var goldenFileStream =
                                        Utility.Utility.GetStream(localGoldenFileName, Constants.FileModeLocal);
                                    client.UploadFile(goldenFileStream.Stream, remoteGoldenFileName);
                                    goldenFileStream.Close();

                                    var versionFileStream =
                                        Utility.Utility.GetStream(localVersionFileName, Constants.FileModeLocal);
                                    client.UploadFile(versionFileStream.Stream, remoteVersionFileName);
                                    versionFileStream.Close();
                                }
                                catch
                                {
                                    throw new Exception(
                                        $"Could not write files to remote {remoteGoldenFileName} {remoteVersionFileName}");
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

        public static void PurgeReplicationFiles()
        {
            // set triggers for async file write
            LastWriteTime = DateTime.Now;
            PendingWrites = true;
        }
    }
}