using System.Collections.Generic;
using PluginFileReader.API.Factory;

namespace PluginFileReader.Helper
{
    public class RootPathFilesObject
    {
        public RootPathObject Root { get; set; }
        public List<string> Paths { get; set; }
    }
}