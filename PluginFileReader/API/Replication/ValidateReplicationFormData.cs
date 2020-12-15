using System.Collections.Generic;
using PluginFileReader.DataContracts;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        public static List<string> ValidateReplicationFormData(this ConfigureReplicationFormData data)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(data.GoldenRecordFileDirectory))
            {
                errors.Add("Golden Record file directory is empty.");
            }
            
            if (string.IsNullOrWhiteSpace(data.GoldenRecordFileName))
            {
                errors.Add("Golden Record file name is empty.");
            }
            
            if (string.IsNullOrWhiteSpace(data.VersionRecordFileDirectory))
            {
                errors.Add("Version Record file directory is empty.");
            }
            
            if (string.IsNullOrWhiteSpace(data.VersionRecordFileName))
            {
                errors.Add("Version Record file name is empty.");
            }

            if (string.IsNullOrWhiteSpace(data.NullValue))
            {
                data.NullValue = "";
            }

            if (string.IsNullOrWhiteSpace(data.CustomHeader))
            {
                data.CustomHeader = "";
            }

            return errors;
        }
    }
}