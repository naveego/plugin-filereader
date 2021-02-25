using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Newtonsoft.Json;
using PluginFileReader.API.Utility;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace PluginFileReader.Helper
{
    public class Settings
    {
        public string GlobalColumnsConfigurationFile { get; set; }
        public string FtpHostname { get; set; }
        public int? FtpPort { get; set; } = 21;
        public string FtpUsername { get; set; }
        public string FtpPassword { get; set; }
        public string FtpSshKey { get; set; }
        public List<RootPathObject> RootPaths { get; set; }

        /// <summary>
        /// Validates the settings input object
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Validate()
        {
            if (RootPaths.Count > 0)
            {
                if (!RootPathsAreDirectories())
                {
                    throw new Exception("A RootPath is not a directory");
                }

                if (!ModeIsSetOnAllRootPaths())
                {
                    throw new Exception("A RootPath does not have a Mode set");
                }

                if (!ArchivePathIsSetOnAllRootPaths())
                {
                    throw new Exception(
                        "A RootPath does not have an Archive Path set when the Clean Up Action is Archive");
                }

                if (!ColumnsValidOnFixedWidthColumnsRootPaths())
                {
                    throw new Exception("A RootPath set to Fixed Width Columns has no columns defined");
                }

                if (!RemoteHasRequiredPermissions().Result)
                {
                    throw new Exception(
                        "A Remote RootPath does not have the required permissions to perform actions configured");
                }
            }
        }

        /// <summary>
        /// Gets all files from location defined by RootPath and Filters and returns in a dictionary by directory
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<string>> GetAllFilesByRootPath(int limitPerRootPath = -1)
        {
            var filesByRootPath = new Dictionary<string, List<string>>();
            foreach (var rootPath in RootPaths)
            {
                var directoryPath = rootPath.RootPath;
                var rootPathName = rootPath.RootPathName();

                if (rootPath.FileReadMode != Constants.FileModeLocal)
                {
                    directoryPath = Path.Join(Utility.TempDirectory, rootPath.RootPath);
                    LoadRemoteFilesIntoTempDirectory(rootPath, limitPerRootPath).Wait();
                }

                if (filesByRootPath.TryGetValue(rootPathName, out var existingFiles))
                {
                    existingFiles.AddRange(Directory.GetFiles(directoryPath, rootPath.Filter));
                }
                else
                {
                    var files = new List<string>();
                    files.AddRange(Directory.GetFiles(directoryPath, rootPath.Filter));

                    if (!filesByRootPath.TryAdd(rootPathName, files))
                    {
                        filesByRootPath[rootPathName].AddRange(files);
                    }
                }
            }

            return filesByRootPath;
        }


        private static readonly SemaphoreSlim LoadRemoteSemaphoreSlim = new SemaphoreSlim(1, 1);

        private async Task LoadRemoteFilesIntoTempDirectory(RootPathObject rootPath, int limit = -1)
        {
            if (rootPath.FileReadMode != Constants.FileModeLocal)
            {
                try
                {
                    await LoadRemoteSemaphoreSlim.WaitAsync();

                    var filesPulled = 0;

                    var tempDirectory = Path.Join(Utility.TempDirectory, rootPath.RootPath);
                    Directory.CreateDirectory(tempDirectory);

                    switch (rootPath.FileReadMode)
                    {
                        case Constants.FileModeFtp:
                            using (var client = Utility.GetFtpClient(this))
                            {
                                var mask = new Regex(rootPath.Filter
                                    .Replace(".", "[.]")
                                    .Replace("*", ".*")
                                    .Replace("?", "."));

                                foreach (var item in await client.GetListingAsync(rootPath.RootPath))
                                {
                                    // if this is a file
                                    if (item.Type == FtpFileSystemObjectType.File && mask.IsMatch(item.Name))
                                    {
                                        if (limit != -1 && filesPulled == limit)
                                        {
                                            break;
                                        }
                                        
                                        var status = await client.DownloadFileAsync(
                                            Path.Combine(tempDirectory, item.Name),
                                            item.FullName);
                                        if (status == FtpStatus.Failed)
                                        {
                                            throw new Exception($"Could not download target file {item.FullName}");
                                        }

                                        filesPulled++;

                                        Logger.Debug($"Downloaded file {item.Name}");
                                    }
                                }

                                await client.DisconnectAsync();
                            }

                            break;
                        case Constants.FileModeSftp:
                            using (var client = Utility.GetSftpClient(this))
                            {
                                var mask = new Regex(rootPath.Filter
                                    .Replace(".", "[.]")
                                    .Replace("*", ".*")
                                    .Replace("?", "."));

                                var allFiles = client.ListDirectory(rootPath.RootPath);
                                var downloadFiles = allFiles.Where(f => f.IsDirectory == false && mask.IsMatch(f.Name));

                                foreach (var file in downloadFiles)
                                {
                                    if (limit != -1 && filesPulled == limit)
                                    {
                                        break;
                                    }
                                    
                                    using (var targetFile = File.Create(Path.Combine(tempDirectory, file.Name)))
                                    {
                                        client.DownloadFile(file.FullName, targetFile);

                                        filesPulled++;
                                        
                                        Logger.Debug($"Downloaded file {file.Name}");
                                    }
                                }

                                client.Disconnect();
                            }

                            break;
                    }
                }
                finally
                {
                    LoadRemoteSemaphoreSlim.Release();
                }
            }
        }

        /// <summary>
        /// Gets all root paths present in a query string
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<RootPathObject> GetRootPathsFromQuery(string query)
        {
            var fromSplits = query.ToLower().Split("from");
            var joinSplits = query.ToLower().Split("join");

            Logger.Debug($"From splits {JsonConvert.SerializeObject(fromSplits, Formatting.Indented)}");
            Logger.Debug($"Join splits {JsonConvert.SerializeObject(joinSplits, Formatting.Indented)}");

            var rootPaths = new List<RootPathObject>();
            foreach (var selectSplit in fromSplits.Skip(1))
            {
                var selectTableSplit = selectSplit.Split(' ').Skip(1).First();

                if (selectTableSplit.Contains('\n'))
                {
                    selectTableSplit = selectTableSplit.Split('\n').First();
                }

                var tableName = selectTableSplit.TrimStart('[').TrimEnd(']');
                rootPaths.Add(RootPaths.Find(r => new DirectoryInfo(r.RootPath).Name.ToLower() == tableName));
            }

            foreach (var joinSplit in joinSplits.Skip(1))
            {
                var joinTableSplit = joinSplit.Split(' ').Skip(1).First();

                if (joinTableSplit.Contains('\n'))
                {
                    joinTableSplit = joinTableSplit.Split('\n').First();
                }

                var tableName = joinTableSplit.TrimStart('[').TrimEnd(']');
                rootPaths.Add(RootPaths.Find(r => new DirectoryInfo(r.RootPath).Name.ToLower() == tableName));
            }

            return rootPaths.Where(r => r != null).GroupBy(r => r.RootPath).Select(g => g.First()).ToList();
        }

        /// <summary>
        /// Reads the columns configuration files if defined on each RootPath and populates the columns property
        /// </summary>
        public void ReconcileColumnsConfigurationFiles()
        {
            if (RootPaths == null)
            {
                RootPaths = new List<RootPathObject>();
            }

            var globalConfigurationColumns = new Dictionary<string, List<Column>>();
            var serializer = new JsonSerializer();

            // load global config file if defined
            if (!string.IsNullOrWhiteSpace(GlobalColumnsConfigurationFile))
            {
                using var file = File.OpenText(GlobalColumnsConfigurationFile);
                globalConfigurationColumns =
                    (Dictionary<string, List<Column>>) serializer.Deserialize(file,
                        typeof(Dictionary<string, List<Column>>));
            }

            // apply config files
            foreach (var rootPath in RootPaths)
            {
                if (rootPath.Mode == Constants.ModeFixedWidth)
                {
                    // apply global config file
                    var indexName = string.IsNullOrWhiteSpace(rootPath.Name)
                        ? new DirectoryInfo(rootPath.RootPath).Name
                        : rootPath.Name;
                    if (globalConfigurationColumns.ContainsKey(indexName))
                    {
                        rootPath.ModeSettings.FixedWidthSettings.Columns = globalConfigurationColumns[indexName];
                    }

                    // apply local config file
                    if (!string.IsNullOrWhiteSpace(rootPath.ModeSettings.FixedWidthSettings.ColumnsConfigurationFile))
                    {
                        using var file =
                            File.OpenText(rootPath.ModeSettings.FixedWidthSettings.ColumnsConfigurationFile);
                        rootPath.ModeSettings.FixedWidthSettings.Columns =
                            (List<Column>) serializer.Deserialize(file, typeof(List<Column>));
                    }
                }
            }
        }

        /// <summary>
        /// Reads the columns configuration files if defined on each RootPath and populates the columns property
        /// </summary>
        public void ReconcileAS400FormatsFiles()
        {
            if (RootPaths == null)
            {
                RootPaths = new List<RootPathObject>();
            }

            var serializer = new JsonSerializer();

            // apply config files
            foreach (var rootPath in RootPaths)
            {
                if (rootPath.Mode == Constants.ModeAS400)
                {
                    // apply local config file
                    if (!string.IsNullOrWhiteSpace(rootPath.ModeSettings.AS400Settings.AS400FormatsConfigurationFile))
                    {
                        using var file =
                            File.OpenText(rootPath.ModeSettings.AS400Settings.AS400FormatsConfigurationFile);
                        rootPath.ModeSettings.AS400Settings.Formats =
                            (List<AS400Format>) serializer.Deserialize(file, typeof(List<AS400Format>));
                    }
                }
            }
        }

        /// <summary>
        /// Adds legacy config support
        /// </summary>
        public void ConvertLegacySettings()
        {
            if (RootPaths != null)
            {
                foreach (var rootPath in RootPaths)
                {
                    if (string.IsNullOrWhiteSpace(rootPath.FileReadMode))
                    {
                        rootPath.FileReadMode = Constants.FileModeLocal;
                    }

                    if (rootPath.ModeSettings == null)
                    {
                        rootPath.ModeSettings = new ModeSettings();
                    }

                    if (rootPath.Mode == Constants.ModeDelimited && rootPath.ModeSettings.DelimitedSettings == null)
                    {
                        rootPath.ModeSettings.DelimitedSettings = new DelimitedSettings
                        {
                            Delimiter = rootPath.Delimiter,
                            HasHeader = rootPath.HasHeader
                        };
                        continue;
                    }

                    if (rootPath.Mode == Constants.ModeFixedWidth && rootPath.ModeSettings.FixedWidthSettings == null)
                    {
                        rootPath.ModeSettings.FixedWidthSettings = new FixedWidthSettings
                        {
                            Columns = rootPath.Columns,
                            ColumnsConfigurationFile = rootPath.ColumnsConfigurationFile
                        };
                        continue;
                    }

                    if (rootPath.Mode == Constants.ModeExcel && rootPath.ModeSettings.ExcelModeSettings == null)
                    {
                        rootPath.ModeSettings.ExcelModeSettings = new ExcelModeSettings
                        {
                            ExcelCells = rootPath.ExcelCells,
                            ExcelColumns = rootPath.ExcelColumns,
                            HasHeader = rootPath.HasHeader
                        };
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if RootPaths are directories
        /// </summary>
        /// <returns></returns>
        private bool RootPathsAreDirectories()
        {
            foreach (var rootPath in RootPaths)
            {
                if (rootPath.FileReadMode == Constants.FileModeLocal && !Directory.Exists(rootPath.RootPath))
                {
                    throw new Exception($"{rootPath.RootPath} is not a directory");
                }

                if (rootPath.FileReadMode == Constants.FileModeFtp)
                {
                    using (var client = Utility.GetFtpClient(this))
                    {
                        try
                        {
                            if (!client.DirectoryExists(rootPath.RootPath))
                            {
                                throw new Exception($"{rootPath.RootPath} is not a directory on remote FTP");
                            }

                            if (rootPath.CleanupAction == Constants.CleanupActionArchive)
                            {
                                if (!client.DirectoryExists(rootPath.ArchivePath))
                                {
                                    throw new Exception($"{rootPath.ArchivePath} is not a directory on remote FTP");
                                }
                            }
                        }
                        finally
                        {
                            client.Disconnect();
                        }
                    }
                }

                if (rootPath.FileReadMode == Constants.FileModeSftp)
                {
                    using (var client = Utility.GetSftpClient(this))
                    {
                        try
                        {
                            if (!client.Exists(rootPath.RootPath))
                            {
                                throw new Exception($"{rootPath.RootPath} is not a directory on remote SFTP");
                            }

                            if (rootPath.CleanupAction == Constants.CleanupActionArchive)
                            {
                                if (!client.Exists(rootPath.ArchivePath))
                                {
                                    throw new Exception($"{rootPath.ArchivePath} is not a directory on remote FTP");
                                }
                            }
                        }
                        finally
                        {
                            client.Disconnect();
                        }
                    }
                }
            }

            return true;
        }

        private bool ModeIsSetOnAllRootPaths()
        {
            foreach (var rootPath in RootPaths)
            {
                if (string.IsNullOrEmpty(rootPath.Mode))
                {
                    throw new Exception($"{rootPath.RootPath} does not have a Mode set");
                }
            }

            return true;
        }

        private bool ArchivePathIsSetOnAllRootPaths()
        {
            foreach (var rootPath in RootPaths)
            {
                if (rootPath.CleanupAction == Constants.CleanupActionArchive &&
                    string.IsNullOrEmpty(rootPath.ArchivePath))
                {
                    throw new Exception($"{rootPath.RootPath} is set to Archive and does not have an Archive Path set");
                }
            }

            return true;
        }

        private bool ColumnsValidOnFixedWidthColumnsRootPaths()
        {
            foreach (var rootPath in RootPaths)
            {
                if (rootPath.Mode == Constants.ModeFixedWidth)
                {
                    if (rootPath.ModeSettings.FixedWidthSettings.Columns.Count == 0)
                    {
                        throw new Exception(
                            $"{rootPath.RootPath} is set to Fixed Width Columns and has no Columns defined");
                    }
                }
            }

            return true;
        }

        private static readonly SemaphoreSlim UploadRemoteSemaphoreSlim = new SemaphoreSlim(1, 1);

        private async Task<bool> RemoteHasRequiredPermissions()
        {
            foreach (var rootPath in RootPaths)
            {
                if (rootPath.FileReadMode != Constants.FileModeLocal)
                {
                    // check read permissions by attempting to download all target files
                    await LoadRemoteFilesIntoTempDirectory(rootPath, 1);

                    try
                    {
                        await UploadRemoteSemaphoreSlim.WaitAsync();

                        // check write permissions to configured archive directories by writing a test file and deleting it
                        if (rootPath.CleanupAction == Constants.CleanupActionArchive)
                        {
                            var testFileName = "test.txt";
                            var remoteTestFileName = Path.Join(rootPath.ArchivePath, testFileName);
                            var localTestDirectory = Path.Join(Utility.TempDirectory, rootPath.ArchivePath);
                            var localTestFileName =
                                Path.Join(Utility.TempDirectory, rootPath.ArchivePath, testFileName);

                            Directory.CreateDirectory(localTestDirectory);
                            var testFile = new StreamWriter(localTestFileName);
                            await testFile.WriteLineAsync("test");
                            testFile.Close();

                            switch (rootPath.FileReadMode)
                            {
                                case Constants.FileModeFtp:
                                    using (var client = Utility.GetFtpClient(this))
                                    {
                                        try
                                        {
                                            var status = await client.UploadFileAsync(localTestFileName,
                                                remoteTestFileName);
                                            if (status == FtpStatus.Failed)
                                            {
                                                throw new Exception(
                                                    $"Could not write to archive directory {rootPath.ArchivePath}");
                                            }

                                            Utility.DeleteFileAtPath(localTestFileName, rootPath, this, true);
                                        }
                                        finally
                                        {
                                            await client.DisconnectAsync();
                                        }
                                    }

                                    break;
                                case Constants.FileModeSftp:
                                    using (var client = Utility.GetSftpClient(this))
                                    {
                                        try
                                        {
                                            var fileStream = Utility.GetFileStream(localTestFileName);
                                            client.UploadFile(fileStream, remoteTestFileName);
                                            Utility.DeleteFileAtPath(localTestFileName, rootPath, this, true);
                                        }
                                        catch
                                        {
                                            throw new Exception(
                                                $"Could not write to archive directory {rootPath.ArchivePath}");
                                        }
                                        finally
                                        {
                                            client.Disconnect();
                                        }
                                    }

                                    break;
                                default:
                                    continue;
                            }
                        }
                    }
                    finally
                    {
                        UploadRemoteSemaphoreSlim.Release();
                    }
                }
            }

            return true;
        }
    }

    public class RootPathObject
    {
        public string RootPath { get; set; }
        public string Filter { get; set; }
        public string Name { get; set; }
        public string CleanupAction { get; set; }
        public string ArchivePath { get; set; }
        public string FileReadMode { get; set; }
        public string Mode { get; set; }
        public int SkipLines { get; set; }

        // MODE SETTINGS
        public ModeSettings ModeSettings { get; set; }

        // LEGACY DELIMITED MODE SETTINGS
        public bool HasHeader { get; set; }
        public string Delimiter { get; set; }

        // LEGACY COLUMN WIDTH MODE SETTINGS
        public string ColumnsConfigurationFile { get; set; }
        public List<Column> Columns { get; set; }

        // LEGACY EXCEL FILE MODE SETTINGS
        public string ExcelColumns { get; set; }
        public List<ExcelCell> ExcelCells { get; set; }

        public string RootPathName()
        {
            return string.IsNullOrWhiteSpace(Name) ? RootPath : Name;
        }
    }

    public class ModeSettings
    {
        // DELIMITED MODE SETTINGS
        public DelimitedSettings DelimitedSettings { get; set; }

        // FIXED WIDTH MODE SETTINGS
        public FixedWidthSettings FixedWidthSettings { get; set; }

        // EXCEL FILE MODE SETTINGS
        public ExcelModeSettings ExcelModeSettings { get; set; }

        // AS400 MODE SETTINGS
        public AS400Settings AS400Settings { get; set; }

        // XML MODE SETTINGS
        public XmlSettings XMLSettings { get; set; }
    }

    public class DelimitedSettings
    {
        public bool HasHeader { get; set; }
        public string Delimiter { get; set; }

        public char GetDelimiter()
        {
            switch (Delimiter)
            {
                case "\\t":
                    return '\t';
                default:
                    return char.Parse(Delimiter);
            }
        }
    }

    public class FixedWidthSettings
    {
        public string ColumnsConfigurationFile { get; set; }
        public List<Column> Columns { get; set; }
    }

    public class ExcelModeSettings
    {
        public bool HasHeader { get; set; }
        public string ExcelColumns { get; set; }
        public List<ExcelCell> ExcelCells { get; set; }

        public List<int> GetAllExcelColumnIndexes()
        {
            if (string.IsNullOrWhiteSpace(ExcelColumns))
            {
                return new List<int>();
            }

            return ExcelColumns.Replace(" ", "").Split(',')
                .Select(x => x.Split('-'))
                .Select(p => new {First = int.Parse(p.First()), Last = int.Parse(p.Last())})
                .SelectMany(x => Enumerable.Range(x.First, x.Last - x.First + 1))
                .OrderBy(z => z).ToList();
        }

        public List<ExcelCell> GetOrderedExcelCells()
        {
            return ExcelCells != null
                ? ExcelCells.OrderBy(x => x.RowIndex).ThenBy(x => x.ColumnIndex).ToList()
                : new List<ExcelCell>();
        }
    }

    public class AS400Settings
    {
        public string AS400FormatsConfigurationFile { get; set; }
        public int KeyValueWidth { get; set; }
        public List<AS400Format> Formats { get; set; }
    }

    public class Column
    {
        public string ColumnName { get; set; }
        public bool IsKey { get; set; }
        public bool IsHeader { get; set; }
        public bool IsGlobalHeader { get; set; }
        public int ColumnStart { get; set; }
        public int ColumnEnd { get; set; }
        public bool TrimWhitespace { get; set; }
    }

    public class ExcelCell
    {
        public string ColumnName { get; set; }
        public int ColumnIndex { get; set; }
        public int RowIndex { get; set; }

        public string GetUniqueName()
        {
            return $"{ColumnName}_{RowIndex}_{ColumnIndex}";
        }
    }

    public class AS400Format
    {
        public AS400KeyValue KeyValue { get; set; }
        public bool SingleRecordPerLine { get; set; }

        public bool IsGlobalHeader { get; set; }

        // single line definition
        public List<Column> Columns { get; set; }

        // if multiline definition 
        public AS400MultiLineDefinition MultiLineDefinition { get; set; }
        public List<string> HeaderRecordKeys { get; set; }
        public List<Column> MultiLineColumns { get; set; }
    }

    public class AS400KeyValue
    {
        // TODO evaluate if needed
        // public int ColumnStart { get; set; }
        // public int ColumnEnd { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
    }

    public class AS400MultiLineDefinition
    {
        public int TagNameStart { get; set; }
        public int TagNameEnd { get; set; }
        public char TagNameDelimiter { get; set; }
        public int ValueLengthStart { get; set; }
        public int ValueLengthEnd { get; set; }
        public int ValueStart { get; set; }
    }

    // XML SETTINGS
    public class XmlSettings
    {
        public List<XmlKey> XmlKeys { get; set; }
    }

    public class XmlKey
    {
        public string ElementId { get; set; }
        public string AttributeId { get; set; }
    }
}