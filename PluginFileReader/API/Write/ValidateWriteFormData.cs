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
                var remoteTargetDirectory = data.GetRemoteTargetDirectory();
                var remoteTargetTestFileName = Path.Join(remoteTargetDirectory, testFileName);
                var localTestDirectory = data.GetLocalTargetDirectory();
                var localTestFileName = Path.Join(localTestDirectory, testFileName);

                Directory.CreateDirectory(localTestDirectory);
                var testFile = new StreamWriter(localTestFileName);
                testFile.WriteLine("test");
                testFile.Close();

                switch (data.FileWriteMode)
                {
                    case Constants.FileModeFtp:
                        using (var client = Utility.Utility.GetFtpClient())
                        {
                            try
                            {
                                if (!client.DirectoryExists(remoteTargetDirectory))
                                {
                                    errors.Add($"{remoteTargetDirectory} is not a directory on remote FTP");
                                }
                                else
                                {
                                    var status = client.UploadFile(localTestFileName, remoteTargetTestFileName);
                                    if (status == FtpStatus.Failed)
                                    {
                                        errors.Add($"Could not write to target directory {remoteTargetDirectory}");
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
                        using (var client = Utility.Utility.GetSftpClient())
                        {
                            try
                            {
                                try
                                {
                                    if (!client.Exists(remoteTargetDirectory))
                                    {
                                        errors.Add($"{remoteTargetDirectory} is not a directory on remote FTP");
                                    }
                                    else
                                    {
                                        var fileStream = Utility.Utility.GetStream(localTestFileName, Constants.FileModeLocal);
                                        client.UploadFile(fileStream.Stream, remoteTargetTestFileName);
                                        fileStream.Close();
                                        Utility.Utility.DeleteFileAtPath(localTestFileName, data, settings, true);
                                    }
                                }
                                catch
                                {
                                    errors.Add($"Could not write to target directory {remoteTargetDirectory}");
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