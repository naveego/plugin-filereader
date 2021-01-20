using System;
using System.Collections.Generic;
using System.IO;
using FluentFTP;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        public static List<string> ValidateReplicationFormData(this ConfigureReplicationFormData data,
            Settings settings)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(data.GoldenRecordFileDirectory))
            {
                errors.Add("Golden Record file directory is empty.");
            }

            if (string.IsNullOrWhiteSpace(data.GoldenRecordFileName))
            {
                errors.Add("Golden Record file name is empty.");
            }

            if (string.IsNullOrWhiteSpace(data.VersionRecordFileDirectory))
            {
                errors.Add("Version Record file directory is empty.");
            }

            if (string.IsNullOrWhiteSpace(data.VersionRecordFileName))
            {
                errors.Add("Version Record file name is empty.");
            }
            
            if (string.IsNullOrWhiteSpace(data.FileWriteMode))
            {
                errors.Add("File Write Mode is empty.");
            }

            if (string.IsNullOrWhiteSpace(data.NullValue))
            {
                data.NullValue = "";
            }

            if (string.IsNullOrWhiteSpace(data.CustomHeader))
            {
                data.CustomHeader = "";
            }

            if (data.FileWriteMode != Constants.FileModeLocal)
            {
                var testFileName = "test.txt";
                    var remoteGoldenTestFileName = Path.Join(data.GetGoldenDirectory(), testFileName);
                    var remoteVersionTestFileName = Path.Join(data.GetVersionDirectory(), testFileName);
                    var localTestFileName = Path.Join(Utility.Utility.TempDirectory, testFileName);
                    
                    var testFile = new StreamWriter(Path.Combine(localTestFileName, testFileName));
                    testFile.WriteLine("test");
                    testFile.Close();
                    
                    switch (data.FileWriteMode)
                    {
                        case Constants.FileModeFtp:
                            using (var client = Utility.Utility.GetFtpClient(settings))
                            {
                                try
                                {
                                    if (!client.DirectoryExists(data.GetGoldenDirectory()))
                                    {
                                        errors.Add($"{data.GetGoldenDirectory()} is not a directory on remote FTP");
                                    }
                                    else
                                    {
                                        var status = client.UploadFile(localTestFileName, remoteGoldenTestFileName);
                                        if (status == FtpStatus.Failed)
                                        {
                                            errors.Add($"Could not write to golden directory {data.GetGoldenDirectory()}");
                                        }
                                    }

                                    if (!client.DirectoryExists(data.GetVersionDirectory()))
                                    {
                                        errors.Add($"{data.GetVersionDirectory()} is not a directory on remote FTP");
                                    }
                                    else
                                    {
                                        var status = client.UploadFile(localTestFileName, remoteVersionTestFileName);
                                        if (status == FtpStatus.Failed)
                                        {
                                            errors.Add($"Could not write to version directory {data.GetVersionDirectory()}");
                                        }
                                    }
                                    
                                    Utility.Utility.DeleteFileAtPath(localTestFileName, data, settings, true);
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
                                    try
                                    {
                                        if (!client.Exists(data.GetGoldenDirectory()))
                                        {
                                            errors.Add($"{data.GetGoldenDirectory()} is not a directory on remote FTP");
                                        }
                                        else
                                        {
                                            var fileStream = Utility.Utility.GetFileStream(localTestFileName);
                                            client.UploadFile(fileStream, remoteGoldenTestFileName);
                                            Utility.Utility.DeleteFileAtPath(localTestFileName, data, settings, true);
                                        }
                                    }
                                    catch
                                    {
                                        errors.Add($"Could not write to golden directory {data.GetGoldenDirectory()}");
                                    }
                                    try
                                    {
                                        if (!client.Exists(data.GetVersionDirectory()))
                                        {
                                            errors.Add($"{data.GetVersionDirectory()} is not a directory on remote FTP");
                                        }
                                        else
                                        {
                                            var fileStream = Utility.Utility.GetFileStream(localTestFileName);
                                            client.UploadFile(fileStream, remoteVersionTestFileName);
                                            Utility.Utility.DeleteFileAtPath(localTestFileName, data, settings, true);
                                        }
                                    }
                                    catch
                                    {
                                        errors.Add($"Could not write to version directory {data.GetVersionDirectory()}");
                                    }
                                }
                                finally
                                {
                                    client.Disconnect();
                                }
                            }
                            
                            break;
                    }
            }
            
            return errors;
        }
    }
}