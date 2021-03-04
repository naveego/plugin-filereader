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
                var goldenTestFileName = "golden_test.txt";
                var remoteGoldenDirectory = data.GetRemoteGoldenDirectory();
                var remoteGoldenTestFileName = Path.Join(remoteGoldenDirectory, goldenTestFileName);
                var localGoldenTestDirectory = data.GetLocalGoldenDirectory();
                var localGoldenTestFileName = Path.Join(localGoldenTestDirectory, goldenTestFileName);
                
                Directory.CreateDirectory(localGoldenTestDirectory);
                var goldenTestFile = new StreamWriter(localGoldenTestFileName);
                goldenTestFile.WriteLine("test");
                goldenTestFile.Close();

                var versionTestFileName = "version_test.txt";
                var remoteVersionDirectory = data.GetRemoteVersionDirectory();
                var remoteVersionTestFileName = Path.Join(remoteVersionDirectory, versionTestFileName);
                var localVersionTestDirectory = data.GetLocalVersionDirectory();
                var localVersionTestFileName = Path.Join(localVersionTestDirectory, versionTestFileName);
                
                Directory.CreateDirectory(localVersionTestDirectory);
                var testFile = new StreamWriter(localVersionTestFileName);
                testFile.WriteLine("test");
                testFile.Close();
                
                switch (data.FileWriteMode)
                {
                    case Constants.FileModeFtp:
                        using (var client = Utility.Utility.GetFtpClient())
                        {
                            try
                            {
                                if (!client.DirectoryExists(remoteGoldenDirectory))
                                {
                                    errors.Add($"{remoteGoldenDirectory} is not a directory on remote FTP");
                                }
                                else
                                {
                                    var status = client.UploadFile(localGoldenTestFileName, remoteGoldenTestFileName);
                                    if (status == FtpStatus.Failed)
                                    {
                                        errors.Add($"Could not write to golden directory {remoteGoldenDirectory}");
                                    }
                                }
                                
                                Utility.Utility.DeleteFileAtPath(localGoldenTestFileName, data, settings, true);

                                if (!client.DirectoryExists(remoteVersionDirectory))
                                {
                                    errors.Add($"{remoteVersionDirectory} is not a directory on remote FTP");
                                }
                                else
                                {
                                    var status = client.UploadFile(localVersionTestFileName, remoteVersionTestFileName);
                                    if (status == FtpStatus.Failed)
                                    {
                                        errors.Add(
                                            $"Could not write to version directory {remoteVersionDirectory}");
                                    }
                                }

                                Utility.Utility.DeleteFileAtPath(localVersionTestFileName, data, settings, true);
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
                                try
                                {
                                    if (!client.Exists(remoteGoldenDirectory))
                                    {
                                        errors.Add($"{remoteGoldenDirectory} is not a directory on remote FTP");
                                    }
                                    else
                                    {
                                        var fileStream = Utility.Utility.GetStream(localGoldenTestFileName, Constants.FileModeLocal);
                                        client.UploadFile(fileStream.Stream, remoteGoldenTestFileName);
                                        fileStream.Close();
                                        Utility.Utility.DeleteFileAtPath(localGoldenTestFileName, data, settings, true);
                                    }
                                }
                                catch
                                {
                                    errors.Add($"Could not write to golden directory {remoteGoldenDirectory}");
                                }

                                try
                                {
                                    if (!client.Exists(remoteVersionDirectory))
                                    {
                                        errors.Add($"{remoteVersionDirectory} is not a directory on remote FTP");
                                    }
                                    else
                                    {
                                        var fileStream = Utility.Utility.GetStream(localVersionTestFileName, Constants.FileModeLocal);
                                        client.UploadFile(fileStream.Stream, remoteVersionTestFileName);
                                        fileStream.Close();
                                        Utility.Utility.DeleteFileAtPath(localVersionTestFileName, data, settings, true);
                                    }
                                }
                                catch
                                {
                                    errors.Add($"Could not write to version directory {remoteVersionDirectory}");
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