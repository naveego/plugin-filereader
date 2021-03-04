using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.Excel
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


        public long WriteLineToFile(string filePathAndName, Dictionary<string, object> recordMap,
            bool includeHeader = false, long lineNumber = -1)
        {
            throw new System.NotImplementedException();
        }

        public List<SchemaTable> GetAllTableNames(string filePathAndName)
        {
            return new List<SchemaTable>
            {
                {
                    new SchemaTable
                    {
                        SchemaName = _schemaName,
                        TableName = _tableName
                    }
                }
            };
        }

        public long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = -1)
        {
            var rowsRead = 0;
            var rowsSkipped = 0;
            List<string> headerColumns = new List<string>();
            List<int> columnIndexes;
            List<ExcelCell> orderedExcelCells = rootPath.ModeSettings.ExcelModeSettings.GetOrderedExcelCells();
            Dictionary<string, object> excelCellsValues = new Dictionary<string, object>();

            using (var stream = File.OpenRead(filePathAndName))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var currentRow = 0;
                    foreach (var cell in orderedExcelCells)
                    {
                        while (reader.Read() && currentRow < cell.RowIndex)
                        {
                            currentRow++;
                        }

                        var rawValue = reader.GetValue(cell.ColumnIndex);

                        if (!excelCellsValues.TryAdd(cell.ColumnName, rawValue))
                        {
                            excelCellsValues.Add(cell.GetUniqueName(), rawValue);
                        }

                        currentRow++;
                    }
                }
            }

            var streamWrapper = Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode);

            using (var reader = ExcelReaderFactory.CreateReader(streamWrapper.Stream))
            {
                // skip lines
                if (rootPath.SkipLines > 0)
                {
                    while (reader.Read() && rowsSkipped < rootPath.SkipLines)
                    {
                        rowsSkipped++;
                    }
                }
                else
                {
                    reader.Read();
                }

                // get desired column indexes
                columnIndexes = rootPath.ModeSettings.ExcelModeSettings.GetAllExcelColumnIndexes();

                if (columnIndexes.Count == 0)
                {
                    columnIndexes = Enumerable.Range(0, reader.FieldCount).ToList();
                }

                // get column names
                foreach (var i in columnIndexes)
                {
                    if (rootPath.ModeSettings.ExcelModeSettings.HasHeader)
                    {
                        var field = reader.GetValue(i)?.ToString();

                        if (string.IsNullOrWhiteSpace(field))
                        {
                            field = $"NO_HEADER_COLUMN_{i}";
                        }

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

                foreach (var cell in excelCellsValues)
                {
                    querySb.Append(
                        $"[{cell.Key}] VARCHAR({int.MaxValue}),");
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

                foreach (var cell in excelCellsValues)
                {
                    querySb.Append($"[{cell.Key}],");
                }

                querySb.Length--;
                querySb.Append(") VALUES (");

                foreach (var column in headerColumns)
                {
                    var paramName = $"@param{headerColumns.IndexOf(column)}";
                    querySb.Append($"{paramName},");
                    cmd.Parameters.Add(paramName);
                }

                foreach (var cell in excelCellsValues)
                {
                    var paramName = $"@param{cell.Key.Replace(" ", "")}";
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
                            var rawValue = reader.GetValue(columnIndexes[headerColumns.IndexOf(column)])
                                ?.ToString();
                            cmd.Parameters[$"@param{headerColumns.IndexOf(column)}"].Value = rawValue;
                        }

                        foreach (var cell in excelCellsValues)
                        {
                            var rawValue = cell.Value?.ToString();
                            cmd.Parameters[$"@param{cell.Key.Replace(" ", "")}"].Value = rawValue;
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
                    
                    // close down stream
                    streamWrapper.Close();
                }
                catch (Exception e)
                {
                    // rollback on error
                    trans.Rollback();
                    Logger.Error(e, e.Message);
                    throw;
                }
            }

            return rowsRead;
        }
    }
}