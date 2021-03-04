using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.AS400
{
    public class AS400ImportExport : IImportExportFile
    {
        private readonly SqlDatabaseConnection _conn;
        private readonly string _tableName;
        private readonly string _schemaName;

        public AS400ImportExport(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            _conn = sqlDatabaseConnection;
            _tableName = tableName;
            _schemaName = schemaName;
        }

        public long ExportTable(string filePathAndName, bool appendToFile = false)
        {
            throw new NotImplementedException();
        }

        public long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = long.MaxValue)
        {
            var formatCmdDictionary = new Dictionary<string, SqlDatabaseCommand>();
            var recordGlobalHeaderMap = new Dictionary<string,string>();
            var recordHeaderMap = new Dictionary<string, Dictionary<string, string>>();
            var recordMap = new Dictionary<string, Dictionary<string, string>>();
            
            var columnsMap = new Dictionary<string, List<Column>>();

            // foreach (var format in AS400.Format25) // POC
            foreach (var format in rootPath.ModeSettings.AS400Settings.Formats)
            {
                if (format.IsGlobalHeader)
                {
                    continue;
                }
                var schemaName = _schemaName;
                var tableName = $"{_tableName}_{format.KeyValue.Name}";

                if (format.SingleRecordPerLine)
                {
                    var columns = new List<Column>(format.Columns);
                    // foreach (var globalFormat in AS400.Format25.FindAll(f => f.IsGlobalHeader)) // POC
                    foreach (var globalFormat in rootPath.ModeSettings.AS400Settings.Formats.FindAll(f => f.IsGlobalHeader))
                    {
                        columns.AddRange(globalFormat.Columns);
                    }
                    
                    CreateTable(schemaName, tableName, columns);
                    formatCmdDictionary[format.KeyValue.Name] = GetInsertCmd(schemaName, tableName, columns);
                    columnsMap[format.KeyValue.Name] = columns;
                }
                else
                {
                    var columns = new List<Column>(format.MultiLineColumns);
                    // foreach (var globalFormat in AS400.Format25.FindAll(f => f.IsGlobalHeader)) // POC
                    foreach (var globalFormat in rootPath.ModeSettings.AS400Settings.Formats.FindAll(f => f.IsGlobalHeader)) 
                    {
                        columns.AddRange(globalFormat.Columns);
                    }
                    CreateTable(schemaName, tableName, columns);
                    formatCmdDictionary[format.KeyValue.Name] = GetInsertCmd(schemaName, tableName, columns);
                    columnsMap[format.KeyValue.Name] = columns;
                }

                recordHeaderMap[format.KeyValue.Name] = new Dictionary<string, string>();
                recordMap[format.KeyValue.Name] = new Dictionary<string, string>();
            }

            // read file into db
            var streamWrapper = Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode);
            var file = streamWrapper.StreamReader;
            string line;
            var recordsInserted = 0;

            var trans = _conn.BeginTransaction();

            try
            {
                // read all lines from file
                while ((line = file.ReadLine()) != null && recordsInserted < limit)
                {
                    // var keyValue = line.Substring(0, 2; // POC
                    var keyValue = line.Substring(0, rootPath.ModeSettings.AS400Settings.KeyValueWidth);
                    // var format = AS400.Format25.FirstOrDefault(f => f.KeyValue.Value == keyValue); // POC
                    var format = rootPath.ModeSettings.AS400Settings.Formats.FirstOrDefault(f => f.KeyValue.Value == keyValue);

                    if (format == null)
                    {
                        continue;
                    }

                    if (format.SingleRecordPerLine)
                    {
                        foreach (var column in format.Columns)
                        {
                            var valueLength = column.ColumnEnd -
                                column.ColumnStart + 1;
                            var rawValue = new string(line.Substring(column.ColumnStart)
                                .Take(valueLength).ToArray());

                            if (format.IsGlobalHeader)
                            {
                                recordGlobalHeaderMap.TryAdd(column.ColumnName, rawValue);
                                continue;
                            }

                            // TODO handle not global header
                        }
                    }
                    else
                    {
                        var tagNameLength = format.MultiLineDefinition.TagNameEnd -
                            format.MultiLineDefinition.TagNameStart + 1;
                        var tagName = line.Substring(format.MultiLineDefinition.TagNameStart, tagNameLength).Trim();
                        var recordId = tagName.Split(format.MultiLineDefinition.TagNameDelimiter).First();
                        var columnName =
                            tagName.Substring(tagName.IndexOf(format.MultiLineDefinition.TagNameDelimiter) + 1);
                        var valueLength = int.Parse(line.Substring(format.MultiLineDefinition.ValueLengthStart,
                            format.MultiLineDefinition.ValueLengthEnd - format.MultiLineDefinition.ValueLengthStart +
                            1));
                        var rawValue = new string(line.Substring(format.MultiLineDefinition.ValueStart)
                            .Take(valueLength).ToArray());

                        bool insert = false;
                        bool headerTrigger = false;
                        bool recordTrigger = false;

                        if (format.HeaderRecordKeys.Contains(recordId))
                        {
                            insert = !recordHeaderMap[format.KeyValue.Name].TryAdd(tagName, rawValue);
                            headerTrigger = insert;
                        }
                        else
                        {
                            if (recordHeaderMap[format.KeyValue.Name].Count != 0)
                            {
                                insert = !recordMap[format.KeyValue.Name].TryAdd(columnName, rawValue);
                                recordTrigger = insert;
                            }
                        }

                        if (insert)
                        {
                            var cmd = formatCmdDictionary[format.KeyValue.Name];
                            if (cmd != null)
                            {
                                var columns = columnsMap[format.KeyValue.Name];
                                foreach (var column in columns)
                                {
                                    string value;

                                    if (column.IsHeader)
                                    {
                                        if (!recordHeaderMap[format.KeyValue.Name]
                                            .TryGetValue(column.ColumnName, out value))
                                        {
                                            value = "";
                                        }

                                        cmd.Parameters[$"@{GetParamName(column)}"].Value =
                                            column.TrimWhitespace ? value.Trim() : value;
                                    }
                                    else if (column.IsGlobalHeader)
                                    {
                                        if (!recordGlobalHeaderMap.TryGetValue(column.ColumnName, out value))
                                        {
                                            value = "";
                                        }

                                        cmd.Parameters[$"@{GetParamName(column)}"].Value =
                                            column.TrimWhitespace ? value.Trim() : value;
                                    }
                                    else
                                    {
                                        if (!recordMap[format.KeyValue.Name]
                                            .TryGetValue(column.ColumnName, out value))
                                        {
                                            value = "";
                                        }

                                        cmd.Parameters[$"@{GetParamName(column)}"].Value =
                                            column.TrimWhitespace ? value.Trim() : value;
                                    }
                                }

                                var hasKey = true;
                                foreach (var keyColumn in format.MultiLineColumns.Where(c => c.IsKey))
                                {
                                    if (string.IsNullOrWhiteSpace(cmd.Parameters[$"@{GetParamName(keyColumn)}"].Value
                                        .ToString()))
                                    {
                                        hasKey = false;
                                        break;
                                    }
                                }
                                
                                // insert record
                                if (recordHeaderMap[format.KeyValue.Name].Count > 0 && hasKey)
                                {
                                    cmd.ExecuteNonQuery();
                                    recordsInserted++;
                                }
                            }


                            // clear storage
                            recordMap[format.KeyValue.Name].Clear();

                            if (headerTrigger)
                            {
                                recordHeaderMap[format.KeyValue.Name].Clear();
                                recordHeaderMap[format.KeyValue.Name].TryAdd(tagName, rawValue);
                            }

                            if (recordTrigger)
                            {
                                recordMap[format.KeyValue.Name].TryAdd(columnName, rawValue);
                            }
                        }
                    }

                    // commit every 1000 rows
                    if (recordsInserted % 1000 == 0)
                    {
                        trans.Commit();
                        trans = _conn.BeginTransaction();
                    }
                }

                // commit any pending inserts
                trans.Commit();
                
                // close down file
                streamWrapper.Close();
            }
            catch (Exception e)
            {
                // rollback on error
                trans.Rollback();
                Logger.Error(e, e.Message);
                throw;
            }

            return recordsInserted;
        }

        public long WriteLineToFile(string filePathAndName, Dictionary<string, object> recordMap, bool includeHeader = false, long lineNumber = -1)
        {
            throw new NotImplementedException();
        }

        public List<SchemaTable> GetAllTableNames(string filePathAndName)
        {
            return new List<SchemaTable>
            {
                {new SchemaTable
                {
                    SchemaName = _schemaName,
                    TableName = _tableName
                }}
            };
        }

        private void CreateTable(string schemaName, string tableName, List<Column> columns)
        {
            // setup db table
            var querySb = new StringBuilder($"CREATE TABLE IF NOT EXISTS [{schemaName}].[{tableName}] (");
            var primaryKeySb = new StringBuilder("PRIMARY KEY (");
            var hasPrimaryKey = false;
            foreach (var column in columns)
            {
                querySb.Append(
                    $"[{column.ColumnName}] VARCHAR({int.MaxValue}){(column.IsKey ? " NOT NULL" : "")},");
                if (column.IsKey)
                {
                    primaryKeySb.Append($"[{column.ColumnName}],");
                    hasPrimaryKey = true;
                }
            }

            if (hasPrimaryKey)
            {
                primaryKeySb.Length--;
                primaryKeySb.Append(")");
                querySb.Append($"{primaryKeySb});");
            }
            else
            {
                querySb.Length--;
                querySb.Append(");");
            }

            var query = querySb.ToString();

            Logger.Debug($"Create table query: {query}");

            var cmd = new SqlDatabaseCommand
            {
                Connection = _conn,
                CommandText = query
            };

            cmd.ExecuteNonQuery();
        }

        private SqlDatabaseCommand GetInsertCmd(string schemaName, string tableName, List<Column> columns)
        {
            var cmd = new SqlDatabaseCommand
            {
                Connection = _conn,
            };

            // prepare insert cmd with parameters
            var querySb = new StringBuilder($"INSERT INTO [{schemaName}].[{tableName}] (");
            foreach (var column in columns)
            {
                querySb.Append($"[{column.ColumnName}],");
            }

            querySb.Length--;
            querySb.Append(") VALUES (");

            foreach (var column in columns)
            {
                var paramName = $"@{GetParamName(column)}";
                querySb.Append($"{paramName},");
                cmd.Parameters.Add(paramName);
            }

            querySb.Length--;
            querySb.Append(");");

            var query = querySb.ToString();

            Logger.Debug($"Insert record query: {query}");

            cmd.CommandText = query;

            return cmd;
        }

        private string GetParamName(Column column)
        {
            return column.ColumnName.Replace(".", "").Replace(" ", "");
        }
    }
}