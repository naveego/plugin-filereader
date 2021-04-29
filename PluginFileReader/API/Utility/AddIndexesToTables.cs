using System.Collections.Generic;
using System.Linq;
using System.Text;
using Naveego.Sdk.Logging;
using Newtonsoft.Json;
using PluginFileReader.API.Factory;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Utility
{
    public static partial class Utility
    {
        public static List<IndexObject> Indexes = new List<IndexObject>();
        
        public static void AddIndexesToTables(IImportExportFile importExportFile, SqlDatabaseConnection conn)
        {
            // get all tables
            var schemaTables = importExportFile.GetAllTableNames();

            foreach (var schemaTable in schemaTables)
            {
                // find index
                var tableIndexes = Indexes?.Where(i => i.TableName.ToLower() == schemaTable.TableName.ToLower()).ToList();

                if (tableIndexes == null || tableIndexes?.Count == 0)
                {
                    return;
                }

                // add all found indexes
                foreach (var index in tableIndexes)
                {
                    // create index command
                    var indexId = $"{index.TableName}_{Indexes.FindIndex(i => i == index)}";
                    var querySb = new StringBuilder($"CREATE INDEX IF NOT EXISTS {indexId} ON {schemaTable.TableName} (");
                
                    // add the columns
                    foreach (var column in index.IndexColumns)
                    {
                        querySb.Append(
                            $"[{column}],");
                    }

                    querySb.Length--;
                    querySb.Append(");");

                    var query = querySb.ToString();

                    Logger.Debug($"Create index query: {query}");

                    var cmd = new SqlDatabaseCommand
                    {
                        Connection = conn,
                        CommandText = query
                    };

                    // execute create index command
                    cmd.ExecuteNonQuery();
                    
                    // rebuild the index
                    query = $"ReIndex {indexId}";
                    
                    cmd = new SqlDatabaseCommand
                    {
                        Connection = conn,
                        CommandText = query
                    };
                    
                    cmd.ExecuteNonQuery();
                    
                    Logger.Info($"Added index {indexId} to {index.TableName} for columns {JsonConvert.SerializeObject(index.IndexColumns)}");
                }   
            }
        }
    }
}