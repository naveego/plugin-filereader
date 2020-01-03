using System;
using System.Collections.Generic;
using System.IO;

namespace PluginCSV.Helper
{
    public class Settings
    {
        public string RootPath { get; set; }
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
            if (String.IsNullOrEmpty(RootPath))
            {
                throw new Exception("the RootPath property must be set");
            }

            if (!RootPathIsDirectory())
            {
                throw new Exception("RootPath is not a directory");
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
            foreach (var fileFilter in Filters)
            {
                files.AddRange(Directory.GetFiles(RootPath, fileFilter));
            }

            return files;
        }
        
        /// <summary>
        /// Checks if RootPath is a directory
        /// </summary>
        /// <returns></returns>
        private bool RootPathIsDirectory()
        {
            try
            {
                return (File.GetAttributes(RootPath) & FileAttributes.Directory) == FileAttributes.Directory;
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
            var files = new List<string>();
            foreach (var fileFilter in Filters)
            {
                files.AddRange(Directory.GetFiles(RootPath, fileFilter));
            }
                    
            if (files.Count == 0)
            {
                return false;
            }

            return true;
        }
    }
}