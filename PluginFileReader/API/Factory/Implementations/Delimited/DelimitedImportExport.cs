using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using Naveego.Sdk.Logging;
using Newtonsoft.Json;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.Delimited
{
    public class DelimitedImportExport : IImportExportFile
    {
        private SqlDatabaseConnection SQLDatabaseConnection { get; set; }
        private string TableName { get; set; }
        private string SchemaName { get; set; }
        private string Delimiter { get; set; }
        private SqlDatabaseTransaction SQLDatabaseTransaction { get; set; } = null;
        private ConfigureReplicationFormData ReplicationFormData { get; set; } = new ConfigureReplicationFormData();
        
        private DelimitedFileReader DelimitedReader { get; set; }
        private DelimitedFileWriter DelimitedWriter { get; set; }

        public DelimitedImportExport(SqlDatabaseConnection sqlDatabaseConnection, string tableName, string schemaName, RootPathObject rootPath)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception("TableName parameter is required.");
            
            if (string.IsNullOrWhiteSpace(schemaName))
                throw new Exception("SchemaName parameter is required.");

            if (sqlDatabaseConnection.State == System.Data.ConnectionState.Closed)
                sqlDatabaseConnection.Open();

            SQLDatabaseConnection = sqlDatabaseConnection;
            TableName = tableName;
            SchemaName = schemaName;
            Delimiter = rootPath.ModeSettings.DelimitedSettings.GetDelimiter();
        }
        
        public DelimitedImportExport(SqlDatabaseConnection sqlDatabaseConnection, string tableName, string schemaName, ConfigureReplicationFormData replicationFormData)
        {
            
            Logger.Info("DelimitedImportExport Constructor: Start");
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception("TableName parameter is required.");
            
            if (string.IsNullOrWhiteSpace(schemaName))
                throw new Exception("SchemaName parameter is required.");

            if (sqlDatabaseConnection.State == System.Data.ConnectionState.Closed)
                sqlDatabaseConnection.Open();

            SQLDatabaseConnection = sqlDatabaseConnection;
            TableName = tableName;
            SchemaName = schemaName;
            Delimiter = replicationFormData.GetDelimiter();
            ReplicationFormData = replicationFormData;
            
            Logger.Info("DelimitedImportExport Constructor: End");
        }
        
        public DelimitedImportExport(SqlDatabaseConnection sqlDatabaseConnection, string tableName, string schemaName, ConfigureWriteFormData writeFormData)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception("TableName parameter is required.");
            
            if (string.IsNullOrWhiteSpace(schemaName))
                throw new Exception("SchemaName parameter is required.");

            if (sqlDatabaseConnection.State == System.Data.ConnectionState.Closed)
                sqlDatabaseConnection.Open();

            SQLDatabaseConnection = sqlDatabaseConnection;
            TableName = tableName;
            SchemaName = schemaName;
            Delimiter = writeFormData.GetDelimiter();
            ReplicationFormData = writeFormData.GetReplicationFormData();
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
                    SchemaName = SchemaName,
                    TableName = TableName
                }}
            };
        }
        
        public long ExportTable(string filePathAndName, bool appendToFile = false)
        {
            SQLDatabaseConnection.Open();
            long rowCount = 0;

            using (SqlDatabaseCommand cmd = new SqlDatabaseCommand(SQLDatabaseConnection))
            {
                if (SQLDatabaseTransaction != null)
                    cmd.Transaction = SQLDatabaseTransaction;

                cmd.CommandText = $@"SELECT * FROM [{SchemaName}].[{TableName}]";
                using (DelimitedWriter = new DelimitedFileWriter(filePathAndName, appendToFile, Encoding.UTF8))
                {
                    // set variables
                    DelimitedWriter.Delimiter = Delimiter;
                    DelimitedWriter.QuoteWrap = ReplicationFormData.QuoteWrap;
                    DelimitedWriter.NullValue = ReplicationFormData.NullValue;
                    
                    // write custom header to file if not empty
                    if (!string.IsNullOrWhiteSpace(ReplicationFormData.CustomHeader))
                    {
                        DelimitedWriter.WriteLineToFile(ReplicationFormData.CustomHeader);
                    }
                    
                    SqlDatabaseDataReader dataReader = cmd.ExecuteReader();
                    List<string> columnNames = new List<string>();
                    // Write header i.e. column names
                    for (int i = 0; i < dataReader.VisibleFieldCount; i++)
                    {
                        var name = dataReader.GetName(i);
                        if (dataReader.GetFieldType(i) != Type.GetType("byte[]") 
                            && name != Constants.ReplicationRecordId
                            && name != Constants.ReplicationVersionIds
                            && name != Constants.ReplicationVersionRecordId) // BLOB will not be written
                        {
                            columnNames.Add(name); //maintain columns in the same order as the header line.
                            DelimitedWriter.AddField(name);
                        }
                    }

                    DelimitedWriter.SaveAndCommitLine();
                    // Write data i.e. rows.                    
                    while (dataReader.Read())
                    {
                        foreach (string columnName in columnNames)
                        {
                            DelimitedWriter.AddField(
                                dataReader.GetString(
                                    dataReader.GetOrdinal(
                                        columnName))); //dataReader.GetOrdinal(ColumnName) provides the position.
                        }

                        DelimitedWriter.SaveAndCommitLine();
                        rowCount++; //Increase row count to track number of rows written.
                    }
                }
            }

            return rowCount;
        }

        public long ImportTable(string filePathAndName, RootPathObject rootPath, bool downloadToLocal = false, long limit = long.MaxValue)
        {
            var autoGenRow = rootPath.ModeSettings.DelimitedSettings.AutoGenRowNumber;
            var includeFileNameAsField = rootPath.ModeSettings.DelimitedSettings.IncludeFileNameAsField;
            var rowCount = 0;
            bool shouldPullRange = !string.IsNullOrWhiteSpace(rootPath.ModeSettings.DelimitedSettings.SelectedRanges);
            
            List<string> headerColumns = new List<string>();
            Dictionary<string, int> selectedColumns = new Dictionary<string, int>() { };

            using (DelimitedReader = new DelimitedFileReader(filePathAndName, rootPath, false))
            {
                DelimitedReader.Delimiter = Delimiter;
                DelimitedReader.OnEmptyLine = BlankLine.SkipEntireLine;
                DelimitedReader.MaximumLines = 1; //Just read one line to get the header info and/or number of columns.
                while (DelimitedReader.ReadLine())
                {
                    int columnCount = 0;
                    
                    string[] fields;
                    
                    if (shouldPullRange)
                    {
                        var selectedRanges = rootPath.ModeSettings.DelimitedSettings.SelectedRanges;
                        var ranges = selectedRanges.Split(',');
                        foreach (var range in ranges)
                        {
                            if (range.Contains('-'))
                            {
                                var ends = range.Split('-');

                                var lowEnd = ends.Length == 1 ? int.Parse(ends[0]) : Math.Min(int.Parse(ends[0]), int.Parse(ends[1]));
                                var highEnd = ends.Length == 1 ? lowEnd : Math.Max(int.Parse(ends[0]), int.Parse(ends[1]));
                                if (highEnd > DelimitedReader.Fields.Length)
                                {
                                    highEnd = DelimitedReader.Fields.Length;
                                }
                                for (int i = lowEnd; i <= highEnd; i++)
                                {
                                    //here ok
                                    selectedColumns.Add(DelimitedReader.Fields[i], i);
                                    
                                }
                            }
                            else
                            {
                                    if (int.Parse(range) < DelimitedReader.Fields.Length)
                                    {
                                        selectedColumns.Add(DelimitedReader.Fields[int.Parse(range)], int.Parse(range));
                                    }
                            }
                        }

                        fields = selectedColumns.Keys.ToArray();
                    }
                    else
                    {
                        fields = DelimitedReader.Fields;
                    }
                    
                    foreach (string field in fields)
                    {
                        columnCount++;
                        if (rootPath.ModeSettings.DelimitedSettings.HasHeader)
                        {
                            if (headerColumns.Contains(field))
                            {
                                headerColumns.Add($"{field}_DUPLICATE_{columnCount}");
                            }
                            else
                            {
                                headerColumns.Add(field);
                            }
                        }
                        else
                            headerColumns.Add("Column" + columnCount);
                    }

                    break;
                }
            }

            if (headerColumns.Count == 0)
                throw new Exception("Columns are required, check the function parameters.");
            
            Logger.Debug($"Headers: {JsonConvert.SerializeObject(headerColumns, Formatting.Indented)}");

            if (SQLDatabaseConnection.State != ConnectionState.Open)
                throw new Exception("A valid and open connection is required.");

            using (SqlDatabaseCommand cmd = new SqlDatabaseCommand(SQLDatabaseConnection))
            {
                if (SQLDatabaseTransaction != null)
                    cmd.Transaction = SQLDatabaseTransaction;

                cmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{SchemaName}].[{TableName}] ({(autoGenRow ? $"[{Constants.AutoRowNum}] INTEGER PRIMARY KEY AUTOINCREMENT," : "")}";

                if (includeFileNameAsField)
                {
                    headerColumns.Insert(0, Constants.AutoFileName);
                }
                
                foreach (var columnName in headerColumns)
                {
                    cmd.CommandText +=
                        $"[{columnName}]" +
                        $" VARCHAR({int.MaxValue}),"; //The DataType none is used since we do not know if all rows have same datatype                        
                }

                cmd.CommandText = cmd.CommandText.Substring(0, cmd.CommandText.Length - 1); //Remove the last comma
                cmd.CommandText += ");";
                Logger.Debug($"Create table SQL: {cmd.CommandText}");
                cmd.ExecuteNonQuery(); // Create table

                var dt = SQLDatabaseConnection.GetSchema("Columns", new string[] {$"[{SchemaName}].[{TableName}]"});
                
                // Sanity check if number of columns in CSV and table are equal
                if (((autoGenRow && dt.Rows.Count - 1 != headerColumns.Count) || (!autoGenRow && dt.Rows.Count != headerColumns.Count)) && !shouldPullRange)
                    throw new Exception("Number of columns in CSV should be same as number of columns in the table");

                // Start of code block to generate INSERT statement.
                var querySb = new StringBuilder($"INSERT INTO [{SchemaName}].[{TableName}] (");
                foreach (string columnName in headerColumns)
                {
                    querySb.Append($"[{columnName}],");
                }
                
                querySb.Length--;
                querySb.Append(") VALUES (");
                
                foreach (string columnName in headerColumns)
                {
                    if (columnName == Constants.AutoFileName && includeFileNameAsField)
                    {
                        var lastIndex = Math.Max(filePathAndName.LastIndexOf('\\'),
                            filePathAndName.LastIndexOf('/')) + 1;
                        var fileName = filePathAndName.Substring(lastIndex, filePathAndName.Length - lastIndex);
                        
                        var paramName = $"{Constants.AutoFileName}";
                        querySb.Append($"\'{fileName}\',");
                        cmd.Parameters.Add(paramName);
                    }
                    else
                    {
                        if (shouldPullRange)
                        {
                            var paramName = $"@param{selectedColumns[columnName]}";
                            querySb.Append($"{paramName},");
                            cmd.Parameters.Add(paramName);
                        }
                        else
                        {
                            var paramName = $"@param{headerColumns.IndexOf(columnName) - Convert.ToInt16(includeFileNameAsField)}";
                            querySb.Append($"{paramName},");
                            cmd.Parameters.Add(paramName);
                        }
                    }
                }

                querySb.Length--;
                querySb.Append(");");

                var query = querySb.ToString();
            
                Logger.Debug($"Insert record query: {query}");

                cmd.CommandText = query;

                // End of code block to generate INSERT statement.

                Logger.Debug($"Reading delimited file {filePathAndName}");
                
                //Read CSV once insert statement has been created.
                using (DelimitedReader = new DelimitedFileReader(filePathAndName, rootPath, downloadToLocal))
                {
                    DelimitedReader.Delimiter = Delimiter;
                    DelimitedReader.OnEmptyLine = BlankLine.SkipEntireLine;
                    DelimitedReader.SkipLines = rootPath.SkipLines;

                    //Skip the header line.
                    if (rootPath.ModeSettings.DelimitedSettings.HasHeader)
                        DelimitedReader.SkipLines += 1;

                    var trans = SQLDatabaseConnection.BeginTransaction();
                    
                    try
                    {
                        
                        
                        while (DelimitedReader.ReadLine() && rowCount < limit)
                        {
                            if (includeFileNameAsField)
                            {
                                var lastIndex = Math.Max(filePathAndName.LastIndexOf('\\'),
                                    filePathAndName.LastIndexOf('/')) + 1;
                                var fileName = filePathAndName.Substring(lastIndex, filePathAndName.Length - lastIndex);
                                cmd.Parameters[Constants.AutoFileName].Value = $"{fileName},";
                            }

                            int csvColumnCount = 0;
                            foreach (string fieldValue in DelimitedReader.Fields)
                            {
                                if (cmd.Parameters.IndexOf("@param" + csvColumnCount) != -1)
                                {
                                    cmd.Parameters["@param" + csvColumnCount].Value =
                                        fieldValue; //Assign File Column to parameter
                                }
                                csvColumnCount++;
                            }

                            cmd.ExecuteNonQuery();
                            rowCount++; // Count inserted rows.
                            
                            // commit every 1000 rows
                            if (rowCount % 1000 == 0)
                            {
                                trans.Commit();
                                trans = SQLDatabaseConnection.BeginTransaction();
                            }
                        }

                        // commit any pending inserts
                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        Logger.Error(e, e.Message);
                        throw;
                    }
                }
            }

            return rowCount;
        }
    }
}