

using Naveego.Sdk.Plugins;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.Helper
{
    public class WriteSettings
    {
        public int CommitSLA { get; set; }
        public Schema Schema { get; set; }
        public ReplicationWriteRequest Replication { get; set; }
        public DataVersions DataVersions { get; set; }
        public SqlDatabaseConnection Connection { get; set; }

        /// <summary>
        /// Returns if mode is set to replication
        /// </summary>
        /// <returns></returns>
        public bool IsReplication()
        {
            return Replication != null;
        }
    }
}