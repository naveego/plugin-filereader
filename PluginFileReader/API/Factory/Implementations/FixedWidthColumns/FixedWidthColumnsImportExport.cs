using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Naveego.Sdk.Logging;
using PluginFileReader.API.Utility;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.FixedWidthColumns
{
    public class FixedWidthColumnsImportExport : IImportExportFile
    {
        private readonly SqlDatabaseConnection _conn;
        private readonly RootPathObject _rootPath;
        private readonly string _tableName;
        private readonly string _schemaName;

        public FixedWidthColumnsImportExport(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            _conn = sqlDatabaseConnection;
            _rootPath = rootPath;
            _tableName = tableName;
            _schemaName = schemaName;
        }

        public long ExportTable(string filePathAndName, bool appendToFile = false)
        {
            throw new System.NotImplementedException();
        }
        
        public long WriteLineToFile(string filePathAndName, Dictionary<string, object> recordMap, bool includeHeader = false, long lineNumber = -1)
        {
            throw new NotImplementedException();
        }

        public List<SchemaTable> GetAllTableNames(bool downloadToLocal = false)
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

        public long ImportTable(string filePathAndName, RootPathObject rootPath, bool downloadToLocal = false, long limit = long.MaxValue)
        {
            var autoGenRow = rootPath.ModeSettings.FixedWidthSettings.AutoGenRowNumber;
            var includeFileNameAsField = rootPath.ModeSettings.FixedWidthSettings.IncludeFileNameAsField;
            
            // setup db table
            var querySb = new StringBuilder($"CREATE TABLE IF NOT EXISTS [{_schemaName}].[{_tableName}] ({(autoGenRow ? $"[{Constants.AutoRowNum}] INTEGER PRIMARY KEY AUTOINCREMENT," : "")}");

            var headerColumns = new List<string>{};
            foreach (var column in rootPath.ModeSettings.FixedWidthSettings.Columns)
            {
                headerColumns.Add(column.ColumnName);
            }
            
            if (includeFileNameAsField)
            {
                headerColumns.Insert(0, Constants.AutoFileName);
                querySb.Append(
                    $"[{Constants.AutoFileName}] VARCHAR({int.MaxValue}),");
            }
            
            foreach (var column in rootPath.ModeSettings.FixedWidthSettings.Columns)
            {
                querySb.Append(
                    $"[{column.ColumnName}] VARCHAR({int.MaxValue}){(column.IsKey ? " NOT NULL UNIQUE" : "")},");
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

            // read file into db
            var streamWrapper = Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode, downloadToLocal);
            var file = streamWrapper.StreamReader;
            var lastIndex = Math.Max(filePathAndName.LastIndexOf('\\'),
                filePathAndName.LastIndexOf('/')) + 1;
            var fileName = filePathAndName.Substring(lastIndex, filePathAndName.Length - lastIndex);

            string line;
            var rowsRead = 0;
            var rowsSkipped = 0;

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
                if (column == Constants.AutoFileName && includeFileNameAsField)
                {
                    var paramName = $"{Constants.AutoFileName}";
                    querySb.Append($"\'{fileName}\',");
                    cmd.Parameters.Add(paramName);
                }
                else
                {
                    var paramName = $"@param{headerColumns.IndexOf(column) - Convert.ToInt16(includeFileNameAsField)}";
                    querySb.Append($"{paramName},");
                    cmd.Parameters.Add(paramName);
                }
                
            }

            querySb.Length--;
            querySb.Append(");");

            query = querySb.ToString();
            
            Logger.Debug($"Insert record query: {query}");

            cmd.CommandText = query;

            var trans = _conn.BeginTransaction();
            
            try
            {
                // skip lines
                if (rootPath.SkipLines > 0)
                {
                    while (file.ReadLine() != null && rowsSkipped < rootPath.SkipLines)
                    {
                        rowsSkipped++;
                    }
                }
                
                // read all lines from file
                while ((line = file.ReadLine()) != null && rowsRead < limit)
                {
                    if (includeFileNameAsField)
                    {
                        cmd.Parameters[$"{Constants.AutoFileName}"].Value = fileName;
                    }
                    
                    foreach (var column in rootPath.ModeSettings.FixedWidthSettings.Columns)
                    {
                        var rawValue = line.Substring(column.ColumnStart, column.ColumnEnd - column.ColumnStart + 1);
                        cmd.Parameters[$"@param{rootPath.ModeSettings.FixedWidthSettings.Columns.IndexOf(column)}"].Value = column.TrimWhitespace ? rawValue.Trim() : rawValue;
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

            return rowsRead;
        }
    }
}