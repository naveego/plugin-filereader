using System;
using System.IO;
using System.Collections.Generic;
using Grpc.Core;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using PluginFileReader.Helper;
using PluginFileReader.API.Utility;

namespace PluginFileReader.API.Read
{
    public static partial class Read
    {
        /// <summary>
        /// Outputs the information on all files to be read as records
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filePaths"></param>
        /// <returns>Records containing the file information</returns>
        public static IEnumerable<Record> ReadFileInfo(List<RootPathFilesObject> pathTargets)
        {
            Logger.Info($"Collecting target file information...");

            var pathSlashChar = '/';

            foreach (var r in pathTargets)
            {
                if (r.Paths.Count > 0)
                {
                    if (r.Root.FileReadMode == Constants.FileModeLocal)
                    {
                        if (Environment.OSVersion.VersionString.ToString().ToLower().Contains("windows"))
                        pathSlashChar = '\\'; // use backslash for windows paths

                        foreach (var p in r.Paths)
                        {
                            if (string.IsNullOrWhiteSpace(p)) continue; // current path empty

                            var record = FileInfoData.CreateFromLocalPath(string.Join(pathSlashChar,
                                r.Root.RootPath, p));
                            
                            if (record != null)
                            {
                                yield return record;
                            }
                        }
                    }
                    else throw new NotImplementedException();
                }
            }
        }
    }
}