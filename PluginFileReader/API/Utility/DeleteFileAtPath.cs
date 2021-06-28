using System;
using System.IO;
using Naveego.Sdk.Logging;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility 
    {
        public static void DeleteFileAtPath(string path, RootPathObject rootPath, bool deleteRemote)
        {
            DeleteFileAtPath(path, rootPath.FileReadMode, deleteRemote);
        }
        
        public static void DeleteFileAtPath(string path, ConfigureReplicationFormData config,  bool deleteRemote)
        {
            DeleteFileAtPath(path, config.FileWriteMode, deleteRemote);
        }
        
        public static void DeleteFileAtPath(string path, ConfigureWriteFormData config, bool deleteRemote)
        {
            DeleteFileAtPath(path, config.FileWriteMode,  deleteRemote);
        }
        
        private static void DeleteFileAtPath(string path, string mode, bool deleteRemote)
        {
            try
            {
                File.Delete(path);

                if (deleteRemote)
                {
                    var remoteFilePath = Path.Join("/", path.Replace(TempDirectory, ""));
                    
                    switch (mode)
                    {
                        case Constants.FileModeFtp:
                            using (var client = GetFtpClient())
                            {
                                try
                                {
                                    client.DeleteFile(remoteFilePath);
                                }
                                finally
                                {
                                    client.Disconnect();
                                }
                            }

                            break;
                        case Constants.FileModeSftp:
                            using (var client = GetSftpClient())
                            {
                                try
                                {
                                    client.DeleteFile(remoteFilePath);
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
            catch (Exception e)
            {
                Logger.Error(e, $"Unable to delete file {path}");
                Logger.Error(e, e.Message);
            }
        }
    }
}