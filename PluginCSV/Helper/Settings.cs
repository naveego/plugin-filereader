using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PluginCSV.Helper
{
    public class Settings
    {
        public List<string> RootPaths { get; set; }
        public List<string> Filters { get; set; }
        public bool HasHeader { get; set; }
        public char Delimiter { get; set; }
        public string CleanupAction { get; set; }
        public string ArchivePath { get; set; }


        /// <summary>
        /// Validates the settings input object
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Validate()
        {
            if (RootPaths.Count == 0)
            {
                throw new Exception("At least one RootPath must be defined");
            }

            if (!RootPathsAreDirectories())
            {
                throw new Exception("A RootPath is not a directory");
            }

            if (!FilesExistAtRootPathsAndFilters())
            {
                throw new Exception("No files in given RootPaths with given Filters");
            }
        }

        /// <summary>
        /// Gets all files from location defined by RootPath and Filters and returns in a flat list
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllFiles()
        {
            var files = new List<string>();
            foreach (var rootPath in RootPaths)
            {
                foreach (var fileFilter in Filters)
                {
                    files.AddRange(Directory.GetFiles(rootPath, fileFilter));
                }
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
                var files = new List<string>();
                foreach (var fileFilter in Filters)
                {
                    files.AddRange(Directory.GetFiles(rootPath, fileFilter));
                }
                filesByDirectory.Add(rootPath, files);
            }
            
            return filesByDirectory;
        }
        
        /// <summary>
        /// Checks if RootPaths are directories
        /// </summary>
        /// <returns></returns>
        private bool RootPathsAreDirectories()
        {
            foreach (var rootPath in RootPaths)
            {
                if (!Directory.Exists(rootPath))
                {
                    throw new Exception($"{rootPath} is not a directory");
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if files exist for the root path under the filters
        /// </summary>
        /// <returns></returns>
        private bool FilesExistAtRootPathsAndFilters()
        {
            return GetAllFiles().Count != 0;
        }
    }
}