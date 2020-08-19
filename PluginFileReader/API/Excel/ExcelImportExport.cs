using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ExcelDataReader;
using PluginFileReader.API.Factory;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Excel
{
    public class ExcelImportExport : IImportExportFile
    {
        private readonly SqlDatabaseConnection _conn;
        private readonly RootPathObject _rootPath;
        private readonly string _tableName;
        private readonly string _schemaName;

        public ExcelImportExport(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            _conn = sqlDatabaseConnection;
            _rootPath = rootPath;
            _tableName = tableName;
            _schemaName = schemaName;
            
            // required for parsing dos era excel files
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public long ExportTable(string filePathAndName, bool appendToFile = false)
        {
            throw new System.NotImplementedException();
        }


        public long WriteLineToFile(string filePathAndName, Dictionary<string, object> recordMap, bool includeHeader = false, long lineNumber = -1)
        {
            throw new System.NotImplementedException();
        }
        
        public long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = -1)
        {
            var rowsRead = 0;
            var rowsSkipped = 0;
            List<string> headerColumns = new List<string>();
            
            using (var stream = File.OpenRead(filePathAndName))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // skip lines
                    if (rootPath.SkipLines > 0)
                    {
                        while (reader.Read() && rowsSkipped < rootPath.SkipLines)
                        {
                            rowsSkipped++;
                        }
                    }
                    
                    // get column names
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        if (rootPath.HasHeader)
                        {
                            var field = reader.GetString(i);
                            
                            if (headerColumns.Contains(field))
                            {
                                headerColumns.Add($"{field}_DUPLICATE_{i}");
                            }
                            else
                            {
                                headerColumns.Add(field);
                            }
                        }
                        else
                        {
                            headerColumns.Add($"COLUMN_{i}");
                        }
                    }
                    
                    // setup db table
                    var querySb = new StringBuilder($"CREATE TABLE IF NOT EXISTS [{_schemaName}].[{_tableName}] (");

                    foreach (var column in headerColumns)
                    {
                        querySb.Append(
                            $"[{column}] VARCHAR({int.MaxValue}),");
                    }

                    querySb.Length--;
                    querySb.Append(");");

                    var query = querySb.ToString();
            
                    Logger.Debug($"Create table query: {query}");

                    var cmd = new SqlDatabaseCommand
                    {
                        Connection = _conn,
                        CommandText = query
                    };

                    cmd.ExecuteNonQuery();
                    
                    // prepare insert cmd with parameters
                    querySb = new StringBuilder($"INSERT INTO [{_schemaName}].[{_tableName}] (");
                    foreach (var column in headerColumns)
                    {
                        querySb.Append($"[{column}],");
                    }

                    querySb.Length--;
                    querySb.Append(") VALUES (");

                    foreach (var column in headerColumns)
                    {
                        var paramName = $"@{column}";
                        querySb.Append($"{paramName},");
                        cmd.Parameters.Add(paramName);
                    }

                    querySb.Length--;
                    querySb.Append(");");

                    query = querySb.ToString();
            
                    Logger.Debug($"Insert record query: {query}");

                    cmd.CommandText = query;
                    
                    // read records
                    var trans = _conn.BeginTransaction();
                    
                    try
                    {
                        // read all lines from file
                        while (reader.Read() && rowsRead < limit)
                        {
                            foreach (var column in headerColumns)
                            {
                                var rawValue = reader.GetString(headerColumns.IndexOf(column));
                                cmd.Parameters[$"@{column}"].Value = rawValue;
                            }

                            cmd.ExecuteNonQuery();

                            rowsRead++;

                            // commit every 1000 rows
                            if (rowsRead % 1000 == 0)
                            {
                                trans.Commit();
                                trans = _conn.BeginTransaction();
                            }
                        }
            
                        // commit any pending inserts
                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        // rollback on error
                        trans.Rollback();
                        Logger.Error(e, e.Message);
                        throw;
                    }
                }
            }

            return rowsRead;
        }
    }
}