using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = long.MaxValue)
        {
            // setup db table
            var querySb = new StringBuilder($"CREATE TABLE IF NOT EXISTS [{_schemaName}].[{_tableName}] (");
            var primaryKeySb = new StringBuilder("PRIMARY KEY (");
            var hasPrimaryKey = false;
            foreach (var column in rootPath.ModeSettings.FixedWidthSettings.Columns)
            {
                querySb.Append(
                    $"[{column.ColumnName}] VARCHAR({int.MaxValue}){(column.IsKey ? " NOT NULL UNIQUE" : "")},");
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

            // read file into db
            var streamWrapper = Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode);
            var file = streamWrapper.StreamReader;
            string line;
            var rowsRead = 0;
            var rowsSkipped = 0;

            // prepare insert cmd with parameters
            querySb = new StringBuilder($"INSERT INTO [{_schemaName}].[{_tableName}] (");
            foreach (var column in rootPath.ModeSettings.FixedWidthSettings.Columns)
            {
                querySb.Append($"[{column.ColumnName}],");
            }

            querySb.Length--;
            querySb.Append(") VALUES (");

            foreach (var column in rootPath.ModeSettings.FixedWidthSettings.Columns)
            {
                var paramName = $"@param{rootPath.ModeSettings.FixedWidthSettings.Columns.IndexOf(column)}";
                querySb.Append($"{paramName},");
                cmd.Parameters.Add(paramName);
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