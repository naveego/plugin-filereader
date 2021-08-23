using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using FluentFTP;
using Naveego.Sdk.Logging;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.FileCopy
{
    public class FileCopyImportExport : IImportExportFile
    {
        private readonly SqlDatabaseConnection _conn;
        private readonly RootPathObject _rootPath;
        private readonly string _tableName;
        private readonly string _schemaName;

        public FileCopyImportExport(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            _conn = sqlDatabaseConnection;
            _rootPath = rootPath;
            _tableName = tableName;
            _schemaName = schemaName;
        }

        public long ExportTable(string filePathAndName, bool appendToFile = false)
        {
            throw new System.NotImplementedException();
        }

        public long WriteLineToFile(string filePathAndName, Dictionary<string, object> recordMap,
            bool includeHeader = false, long lineNumber = -1)
        {
            throw new NotImplementedException();
        }

        public List<SchemaTable> GetAllTableNames(bool downloadToLocal = false)
        {
            return new List<SchemaTable>
            {
                {
                    new SchemaTable
                    {
                        SchemaName = _schemaName,
                        TableName = _tableName
                    }
                }
            };
        }

        public long ImportTable(string filePathAndName, RootPathObject rootPath, bool downloadToLocal = false,
            long limit = long.MaxValue)
        {
            var copySettings = rootPath.ModeSettings.FileCopySettings;
            var runStart = DateTime.Now;
            var runStartTimestamp = runStart.ToString();
            var runSuccess = true;
            var runSourceFile = filePathAndName;
            var runTargetFile = "";
            var runFileSize = "";
            var runError = "";

            var query = @$"
CREATE TABLE IF NOT EXISTS [{_schemaName}].[{_tableName}] (
[RUN_ID] VARCHAR({int.MaxValue}),
[RUN_START_TIMESTAMP] VARCHAR({int.MaxValue}),
[RUN_END_TIMESTAMP] VARCHAR({int.MaxValue}),
[RUN_SUCCESS] VARCHAR({int.MaxValue}),
[RUN_SOURCE_FILE] VARCHAR({int.MaxValue}),
[RUN_TARGET_FILE] VARCHAR({int.MaxValue}),
[RUN_FILE_SIZE] VARCHAR({int.MaxValue}),
[RUN_ERROR] VARCHAR({int.MaxValue})
);";

            Logger.Debug($"Create table query: {query}");

            var cmd = new SqlDatabaseCommand
            {
                Connection = _conn,
                CommandText = query
            };

            cmd.ExecuteNonQuery();

            // check if this is a full read or not
            // copy files on full read only
            if (limit == long.MaxValue && downloadToLocal)
            {
                // delay if configured and needed
                if (copySettings.MinimumSendDelayMS > 0)
                {
                    var delayOffsetMS = Convert.ToInt32((DateTime.Now - FileCopyGlobals.LastWriteTime).TotalMilliseconds);

                    if (copySettings.MinimumSendDelayMS - delayOffsetMS > 0)
                    {
                        Thread.Sleep(copySettings.MinimumSendDelayMS - delayOffsetMS);
                    }
                }
                
                var streamWrapper =
                    Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode, true);
                
                try
                {
                    var inputFileStream = streamWrapper.Stream;
                    var inputFilePathAndName = Utility.Utility.GetTempFilePath(filePathAndName);
                    var targetFilePathAndName = 
                        Path.Join(copySettings.TargetDirectoryPath, Path.GetFileName(filePathAndName));

                    if (!targetFilePathAndName.Contains("\\\\") && targetFilePathAndName.Contains("\\"))
                    {
                        targetFilePathAndName = targetFilePathAndName.Replace('\\', '/');
                    }

                    runTargetFile = targetFilePathAndName;
                    
                    // ftp/sftp base settings to target host
                    var ftpSettings = new FtpSettings
                    {
                        FtpHostname = copySettings.FtpHostname,
                        FtpPort = copySettings.FtpPort,
                        FtpUsername = copySettings.FtpUsername,
                        FtpPassword = copySettings.FtpPassword,
                        FtpSshKey = copySettings.FtpSshKey
                    };

                    switch (copySettings.TargetFileMode)
                    {
                        case Constants.FileModeFtp:
                            using (var client = Utility.Utility.GetFtpClient(ftpSettings))
                            {
                                try
                                {
                                    var status = client.UploadFile(inputFilePathAndName, targetFilePathAndName, copySettings.OverwriteTarget ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip);
                                    if (status == FtpStatus.Failed)
                                    {
                                        runSuccess = false;
                                        runError = $"Could not write file to remote {targetFilePathAndName}, {client.LastReply.ErrorMessage}";
                                    }
                                }
                                finally
                                {
                                    client.Disconnect();
                                }
                            }

                            break;
                        case Constants.FileModeSftp:
                            using (var client = Utility.Utility.GetSftpClient(ftpSettings))
                            {
                                try
                                {
                                    client.UploadFile(inputFileStream, targetFilePathAndName, copySettings.OverwriteTarget);
                                }
                                catch(Exception e)
                                {
                                    runSuccess = false;
                                    runError = $"Could not write file to remote {targetFilePathAndName}, {e.Message}";
                                    Logger.Error(e, e.Message);
                                }
                                finally
                                {
                                    client.Disconnect();
                                }
                            }

                            break;
                        case Constants.FileModeLocal:
                            using (var fileStream = File.Create(targetFilePathAndName))
                            {
                                inputFileStream.Seek(0, SeekOrigin.Begin);
                                inputFileStream.CopyTo(fileStream);
                            }

                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message);
                    runSuccess = false;
                    runError = e.Message;
                }
                finally
                {
                    // calculate file size
                    var totalBytes = streamWrapper.Stream.Length;
                    var suffixes = new List<string>
                    {
                        "B", "KB", "MB", "GB", "TB", "PB", "EB"
                    };
                    
                    if (totalBytes == 0)
                    {
                        runFileSize = "0 B";
                    }
                    else
                    {
                        var bytes = Math.Abs(totalBytes);
                        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                        var num = Math.Round(bytes / Math.Pow(1024, place), 1);
                        runFileSize = $"{Math.Sign(totalBytes) * num}{suffixes[place]}";
                    }
                    
                    // close input file stream
                    streamWrapper.Close();
                }
            }
    
            // insert run status to
            query = $@"
INSERT INTO [{_schemaName}].[{_tableName}] (
[RUN_ID],
[RUN_START_TIMESTAMP],
[RUN_END_TIMESTAMP],
[RUN_SUCCESS],
[RUN_SOURCE_FILE],
[RUN_TARGET_FILE],
[RUN_FILE_SIZE],
[RUN_ERROR]
) VALUES (
@param0,
@param1,
@param2,
@param3,
@param4,
@param5,
@param6,
@param7
);";

            Logger.Debug($"Insert record query: {query}");

            cmd.CommandText = query;

            var trans = _conn.BeginTransaction();

            try
            {
                // set params
                // RUN_ID
                cmd.Parameters.Add("@param0");
                cmd.Parameters[$"@param0"].Value = Guid.NewGuid();
                // RUN_START_TIMESTAMP
                cmd.Parameters.Add("@param1");
                cmd.Parameters[$"@param1"].Value = runStartTimestamp;
                // RUN_END_TIMESTAMP
                cmd.Parameters.Add("@param2");
                cmd.Parameters[$"@param2"].Value = DateTime.Now.ToString();
                // RUN_SUCCESS
                cmd.Parameters.Add("@param3");
                cmd.Parameters[$"@param3"].Value = runSuccess.ToString();
                // RUN_SOURCE_FILE
                cmd.Parameters.Add("@param4");
                cmd.Parameters[$"@param4"].Value = runSourceFile;
                // RUN_TARGET_FILE
                cmd.Parameters.Add("@param5");
                cmd.Parameters[$"@param5"].Value = runTargetFile;
                // RUN_FILE_SIZE
                cmd.Parameters.Add("@param6");
                cmd.Parameters[$"@param6"].Value = runFileSize;
                // RUN_ERROR
                cmd.Parameters.Add("@param7");
                cmd.Parameters[$"@param7"].Value = runError;
                
                cmd.ExecuteNonQuery();
                
                // commit any pending inserts
                trans.Commit();
            }
            catch (Exception e)
            {
                // rollback on error
                trans.Rollback();
                Logger.Error(e, e.Message);
                throw;
            }
            
            FileCopyGlobals.LastWriteTime = DateTime.Now;

            return 1;
        }
    }
}