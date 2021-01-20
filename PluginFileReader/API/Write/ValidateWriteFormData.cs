using System.Collections.Generic;
using System.IO;
using FluentFTP;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        public static List<string> ValidateWriteFormData(this ConfigureWriteFormData data,
            Settings settings)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(data.TargetFileDirectory))
            {
                errors.Add("Target file directory is empty.");
            }

            if (string.IsNullOrWhiteSpace(data.TargetFileName))
            {
                errors.Add("Target file name is empty.");
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
                    var remoteTargetTestFileName = Path.Join(data.GetTargetDirectory(), testFileName);
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
                                    if (!client.DirectoryExists(data.GetTargetDirectory()))
                                    {
                                        errors.Add($"{data.GetTargetDirectory()} is not a directory on remote FTP");
                                    }
                                    else
                                    {
                                        var status = client.UploadFile(localTestFileName, remoteTargetTestFileName);
                                        if (status == FtpStatus.Failed)
                                        {
                                            errors.Add($"Could not write to target directory {data.GetTargetDirectory()}");
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
                                        if (!client.Exists(data.GetTargetDirectory()))
                                        {
                                            errors.Add($"{data.GetTargetDirectory()} is not a directory on remote FTP");
                                        }
                                        else
                                        {
                                            var fileStream = Utility.Utility.GetFileStream(localTestFileName);
                                            client.UploadFile(fileStream, remoteTargetTestFileName);
                                            Utility.Utility.DeleteFileAtPath(localTestFileName, data, settings, true);
                                        }
                                    }
                                    catch
                                    {
                                        errors.Add($"Could not write to target directory {data.GetTargetDirectory()}");
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