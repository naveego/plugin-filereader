namespace PluginFileReader.DataContracts
{
    public class FtpSettings
    {
        public string FtpHostname { get; set; }
        public int? FtpPort { get; set; } = 22;
        public string FtpUsername { get; set; }
        public string FtpPassword { get; set; }
        public string FtpSshKey { get; set; }
    }
}