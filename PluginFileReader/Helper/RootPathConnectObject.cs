using System.Collections.Generic;
using PluginFileReader.API.Factory;

namespace PluginFileReader.Helper
{
    public class RootPathConnectObject
    {
        public RootPathObject RootPathObject { get; set; }
        public IImportExportFactory ImportExportFactory { get; set; }
        public List<string> Paths { get; set; }
    }
}