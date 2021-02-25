using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
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
        private char Delimiter { get; set; }
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

        public List<SchemaTable> GetAllTableNames(string filePathAndName)
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

        public long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = long.MaxValue)
        {
            var rowCount = 0;
            List<string> headerColumns = new List<string>();

            using (DelimitedReader = new DelimitedFileReader(filePathAndName, rootPath))
            {
                DelimitedReader.Delimiter = Delimiter;
                DelimitedReader.OnEmptyLine = BlankLine.SkipEntireLine;
                DelimitedReader.MaximumLines = 1; //Just read one line to get the header info and/or number of columns.
                while (DelimitedReader.ReadLine())
                {
                    int columnCount = 0;
                    foreach (string field in DelimitedReader.Fields)
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

                // cmd.CommandText = $"DROP TABLE IF EXISTS [{SchemaName}].[{TableName}]";
                // cmd.ExecuteNonQuery();
                
                cmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{SchemaName}].[{TableName}] (";
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
                if (dt.Rows.Count != headerColumns.Count)
                    throw new Exception("Number of columns in CSV should be same as number of columns in the table");

                // Start of code block to generate INSERT statement.
                cmd.CommandText = $"INSERT INTO {SchemaName}.[{TableName}] VALUES (";
                int paramCount = 0;
                foreach (string columnName in headerColumns)
                {
                    paramCount++;
                    cmd.CommandText +=
                        $"@param{paramCount},"; //The DataType none is used since we do not know if all rows have same datatype                        
                }

                cmd.CommandText = cmd.CommandText.Substring(0, cmd.CommandText.Length - 1); //Remove the last comma
                cmd.CommandText += ");";
                
                Logger.Debug($"Insert Row SQL: {cmd.CommandText}");

                // Add parameters
                paramCount = 0;
                foreach (string columnName in headerColumns)
                {
                    paramCount++;
                    cmd.Parameters.Add(
                        $"@param{paramCount}"); //The DataType none is used since we do not know if all rows have same datatype                        
                }

                // End of code block to generate INSERT statement.

                Logger.Debug($"Reading delimited file {filePathAndName}");
                
                //Read CSV once insert statement has been created.
                using (DelimitedReader = new DelimitedFileReader(filePathAndName, rootPath))
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
                            int csvColumnCount = 0;
                            foreach (string fieldValue in DelimitedReader.Fields)
                            {
                                csvColumnCount++;
                                if (cmd.Parameters.IndexOf("@param" + csvColumnCount) != -1)
                                {
                                    cmd.Parameters["@param" + csvColumnCount].Value =
                                        fieldValue; //Assign File Column to parameter
                                }
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