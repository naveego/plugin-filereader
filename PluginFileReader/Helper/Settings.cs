using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

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
            if (RootPaths == null)
            {
                RootPaths = new List<RootPathObject>();
            }

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
                // apply global config file
                var indexName = string.IsNullOrWhiteSpace(rootPath.Name)
                    ? new DirectoryInfo(rootPath.RootPath).Name
                    : rootPath.Name;
                if (globalConfigurationColumns.ContainsKey(indexName))
                {
                    rootPath.Columns = globalConfigurationColumns[indexName];
                }

                // apply local config file
                if (!string.IsNullOrWhiteSpace(rootPath.ColumnsConfigurationFile))
                {
                    using var file = File.OpenText(rootPath.ColumnsConfigurationFile);
                    rootPath.Columns = (List<Column>) serializer.Deserialize(file, typeof(List<Column>));
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
                if (rootPath.Mode == "Fixed Width Columns")
                {
                    if (rootPath.Columns.Count == 0)
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
        public string Mode { get; set; }
        public string CleanupAction { get; set; }
        public string ArchivePath { get; set; }

        // FLAT FILE MODE SETTINGS
        public bool HasHeader { get; set; }
        public char Delimiter { get; set; }

        // FIXED COLUMN WIDTH MODE SETTINGS
        public string ColumnsConfigurationFile { get; set; }
        public List<Column> Columns { get; set; }
    }

    public class Column
    {
        public string ColumnName { get; set; }
        public bool IsKey { get; set; }
        public int ColumnStart { get; set; }
        public int ColumnEnd { get; set; }
        public bool TrimWhitespace { get; set; }
    }
}