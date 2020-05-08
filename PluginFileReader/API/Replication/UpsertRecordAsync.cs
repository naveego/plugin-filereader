using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Replication
{
    public static partial class Replication
    {
        public static async Task UpsertRecordAsync(SqlDatabaseConnection conn,
            ReplicationTable table,
            Dictionary<string, object> recordMap)
        {
            await conn.OpenAsync();

            try
            {
                // try to insert
                var querySb =
                    new StringBuilder(
                        $"INSERT INTO {Utility.Utility.GetSafeName(table.SchemaName)}.{Utility.Utility.GetSafeName(table.TableName)}(");
                foreach (var column in table.Columns)
                {
                    querySb.Append($"{Utility.Utility.GetSafeName(column.ColumnName)},");
                }

                querySb.Length--;
                querySb.Append(") VALUES (");

                foreach (var column in table.Columns)
                {
                    if (recordMap.ContainsKey(column.ColumnName))
                    {
                        var rawValue = recordMap[column.ColumnName];
                        if (rawValue == null || string.IsNullOrWhiteSpace(rawValue.ToString()))
                        {
                            querySb.Append($"NULL,");
                        }
                        else
                        {
                            if (column.Serialize)
                            {
                                rawValue = JsonConvert.SerializeObject(rawValue);
                            }

                            querySb.Append($"'{Utility.Utility.GetSafeString(rawValue.ToString())}',");
                        }
                    }
                    else
                    {
                        querySb.Append($"NULL,");
                    }
                }

                querySb.Length--;
                querySb.Append(");");

                var query = querySb.ToString();

                Logger.Debug($"Insert record query: {query}");

                var cmd = new SqlDatabaseCommand
                {
                    Connection = conn,
                    CommandText = query
                };

                await cmd.ExecuteNonQueryAsync();
                
                //TODO: Insert record into file
            }
            catch (Exception e)
            {
                try
                {
                    // update if it failed
                    var querySb =
                        new StringBuilder(
                            $"UPDATE {Utility.Utility.GetSafeName(table.SchemaName)}.{Utility.Utility.GetSafeName(table.TableName)} SET ");
                    foreach (var column in table.Columns)
                    {
                        if (!column.PrimaryKey)
                        {
                            if (recordMap.ContainsKey(column.ColumnName))
                            {
                                var rawValue = recordMap[column.ColumnName];
                                if (rawValue == null || string.IsNullOrWhiteSpace(rawValue.ToString()))
                                {
                                    querySb.Append($"{Utility.Utility.GetSafeName(column.ColumnName)}=NULL,");
                                }
                                else
                                {
                                    if (column.Serialize)
                                    {
                                        rawValue = JsonConvert.SerializeObject(rawValue);
                                    }

                                    querySb.Append(
                                        $"{Utility.Utility.GetSafeName(column.ColumnName)}='{Utility.Utility.GetSafeString(rawValue.ToString())}',");
                                }
                            }
                            else
                            {
                                querySb.Append($"{Utility.Utility.GetSafeName(column.ColumnName)}=NULL,");
                            }
                        }
                    }

                    querySb.Length--;

                    var primaryKey = table.Columns.Find(c => c.PrimaryKey);
                    var primaryValue = recordMap[primaryKey.ColumnName];
                    if (primaryKey.Serialize)
                    {
                        primaryValue = JsonConvert.SerializeObject(primaryValue);
                    }

                    querySb.Append($" WHERE {Utility.Utility.GetSafeName(primaryKey.ColumnName)}='{Utility.Utility.GetSafeString(primaryValue.ToString())}'");

                    var query = querySb.ToString();
                    
                    Logger.Debug($"Update record query: {query}");

                    var cmd = new SqlDatabaseCommand
                    {
                        Connection = conn,
                        CommandText = query
                    };

                    await cmd.ExecuteNonQueryAsync();
                    
                    //TODO: Update record into file
                }
                catch (Exception exception)
                {
                    await conn.CloseAsync();
                    Logger.Error($"Error Insert: {e.Message}");
                    Logger.Error($"Error Update: {exception.Message}");
                    throw;
                }
            }

            await conn.CloseAsync();
        }
    }
}