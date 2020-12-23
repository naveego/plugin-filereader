using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PluginFileReader.API.Utility;

namespace PluginFileReader.Helper
{
    public class Settings
    {
        public string GlobalColumnsConfigurationFile { get; set; }
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

                if (!ColumnsValidOnFixedWidthColumnsRootPaths())
                {
                    throw new Exception("A RootPath set to Fixed Width Columns has no columns defined");
                }
            }
        }

        /// <summary>
        /// Gets all files from location defined by RootPath and Filters and returns in a flat list
        /// </summary>
        /// <returns></returns>
        private List<string> GetAllFiles()
        {
            var files = new List<string>();
            foreach (var rootPath in RootPaths)
            {
                files.AddRange(Directory.GetFiles(rootPath.RootPath, rootPath.Filter));
            }

            return files;
        }

        /// <summary>
        /// Gets all files from location defined by RootPath and Filters and returns in a dictionary by directory
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<string>> GetAllFilesByDirectory()
        {
            var filesByDirectory = new Dictionary<string, List<string>>();
            foreach (var rootPath in RootPaths)
            {
                if (filesByDirectory.TryGetValue(rootPath.RootPath, out var existingFiles))
                {
                    existingFiles.AddRange(Directory.GetFiles(rootPath.RootPath, rootPath.Filter));
                }
                else
                {
                    var files = new List<string>();
                    files.AddRange(Directory.GetFiles(rootPath.RootPath, rootPath.Filter));
                    filesByDirectory.Add(rootPath.RootPath, files);
                }
            }

            return filesByDirectory;
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
                if (rootPath.Mode == Constants.FixedWidthMode)
                {
                    // apply global config file
                    var indexName = string.IsNullOrWhiteSpace(rootPath.Name)
                        ? new DirectoryInfo(rootPath.RootPath).Name
                        : rootPath.Name;
                    if (globalConfigurationColumns.ContainsKey(indexName))
                    {
                        rootPath.FixedWidthSettings.Columns = globalConfigurationColumns[indexName];
                    }

                    // apply local config file
                    if (!string.IsNullOrWhiteSpace(rootPath.FixedWidthSettings.ColumnsConfigurationFile))
                    {
                        using var file = File.OpenText(rootPath.FixedWidthSettings.ColumnsConfigurationFile);
                        rootPath.FixedWidthSettings.Columns = (List<Column>) serializer.Deserialize(file, typeof(List<Column>));
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
                if (rootPath.Mode == Constants.AS400Mode)
                {
                    // apply local config file
                    if (!string.IsNullOrWhiteSpace(rootPath.AS400Settings.AS400FormatsConfigurationFile))
                    {
                        using var file = File.OpenText(rootPath.AS400Settings.AS400FormatsConfigurationFile);
                        rootPath.AS400Settings.Formats = (List<AS400Format>) serializer.Deserialize(file, typeof(List<AS400Format>));
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
                if (!Directory.Exists(rootPath.RootPath))
                {
                    throw new Exception($"{rootPath.RootPath} is not a directory");
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if files exist for the root path under the filters
        /// </summary>
        /// <returns></returns>
        public bool FilesExistAtRootPathsAndFilters()
        {
            return GetAllFiles().Count != 0;
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
                    if (rootPath.Mode == Constants.DelimitedMode && rootPath.DelimitedSettings == null)
                    {
                        rootPath.DelimitedSettings = new DelimitedSettings
                        {
                            Delimiter = rootPath.Delimiter,
                            HasHeader = rootPath.HasHeader
                        };
                        continue;
                    }

                    if (rootPath.Mode == Constants.FixedWidthMode && rootPath.FixedWidthSettings == null)
                    {
                        rootPath.FixedWidthSettings = new FixedWidthSettings
                        {
                            Columns = rootPath.Columns,
                            ColumnsConfigurationFile = rootPath.ColumnsConfigurationFile
                        };
                        continue;    
                    }

                    if (rootPath.Mode == Constants.ExcelMode && rootPath.ExcelModeSettings == null)
                    {
                        rootPath.ExcelModeSettings = new ExcelModeSettings
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

        private bool ColumnsValidOnFixedWidthColumnsRootPaths()
        {
            foreach (var rootPath in RootPaths)
            {
                if (rootPath.Mode == Constants.FixedWidthMode)
                {
                    if (rootPath.FixedWidthSettings.Columns.Count == 0)
                    {
                        throw new Exception(
                            $"{rootPath.RootPath} is set to Fixed Width Columns and has no Columns defined");
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
        public string Mode { get; set; }
        public int SkipLines { get; set; }

        // DELIMITED MODE SETTINGS
        public DelimitedSettings DelimitedSettings {get; set; }
        
        // LEGACY DELIMITED MODE SETTINGS
        public bool HasHeader { get; set; }
        public string Delimiter { get; set; }
        
        // FIXED WIDTH MODE SETTINGS
        public FixedWidthSettings FixedWidthSettings { get; set; }

        // LEGACY COLUMN WIDTH MODE SETTINGS
        public string ColumnsConfigurationFile { get; set; }
        public List<Column> Columns { get; set; }

        // EXCEL FILE MODE SETTINGS
        public ExcelModeSettings ExcelModeSettings { get; set; }
        
        // LEGACY EXCEL FILE MODE SETTINGS
        public string ExcelColumns { get; set; }
        public List<ExcelCell> ExcelCells { get; set; }
        
        // AS400 MODE SETTINGS
        public AS400Settings AS400Settings { get; set; }
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
}