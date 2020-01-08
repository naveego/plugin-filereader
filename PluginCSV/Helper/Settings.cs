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

            if (!FilesExistAtRootPathAndFilters())
            {
                throw new Exception("No files in specified RootPath under given Filters");
            }
        }

        /// <summary>
        /// Gets all files from location defined by RootPath and Filters
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
        /// Checks if RootPaths are directories
        /// </summary>
        /// <returns></returns>
        private bool RootPathsAreDirectories()
        {
            try
            {
                return RootPaths.All(rootPath => (File.GetAttributes(rootPath) & FileAttributes.Directory) == FileAttributes.Directory);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Checks if files exist for the root path under the filters
        /// </summary>
        /// <returns></returns>
        private bool FilesExistAtRootPathAndFilters()
        {
            return GetAllFiles().Count != 0;
        }
    }
}