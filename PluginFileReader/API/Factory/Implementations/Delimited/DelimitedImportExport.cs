using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
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
        private readonly SqlDatabaseConnection _conn;
        private readonly RootPathObject _rootPath;
        private readonly ConfigureReplicationFormData _replicationFormData;
        private readonly string _tableName;
        private readonly string _schemaName;

        public DelimitedImportExport(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            _conn = sqlDatabaseConnection;
            _rootPath = rootPath;
            _tableName = tableName;
            _schemaName = schemaName;
        }

        public DelimitedImportExport(SqlDatabaseConnection sqlDatabaseConnection,
            ConfigureReplicationFormData replicationFormData, string tableName, string schemaName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception("TableName parameter is required.");

            if (string.IsNullOrWhiteSpace(schemaName))
                throw new Exception("SchemaName parameter is required.");

            _conn = sqlDatabaseConnection;
            _tableName = tableName;
            _schemaName = schemaName;
            _replicationFormData = replicationFormData;
        }

        public DelimitedImportExport(SqlDatabaseConnection sqlDatabaseConnection, ConfigureWriteFormData writeFormData,
            string tableName, string schemaName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new Exception("TableName parameter is required.");

            if (string.IsNullOrWhiteSpace(schemaName))
                throw new Exception("SchemaName parameter is required.");

            _conn = sqlDatabaseConnection;
            _tableName = tableName;
            _schemaName = schemaName;
            _replicationFormData = writeFormData.GetReplicationFormData();
        }

        public long ExportTable(string filePathAndName, bool appendToFile = false)
        {
            _conn.Open();
            long rowCount = 0;

            using (SqlDatabaseCommand cmd = new SqlDatabaseCommand(_conn))
            {
                cmd.CommandText = $@"SELECT * FROM [{_schemaName}].[{_tableName}]";
                using (var delimitedWriter = new DelimitedFileWriter(filePathAndName, appendToFile, Encoding.UTF8))
                {
                    // set variables
                    delimitedWriter.Delimiter = _replicationFormData.GetDelimiter();
                    delimitedWriter.QuoteWrap = _replicationFormData.QuoteWrap;
                    delimitedWriter.NullValue = _replicationFormData.NullValue;
                    
                    // write custom header to file if not empty
                    if (!string.IsNullOrWhiteSpace(_replicationFormData.CustomHeader))
                    {
                        delimitedWriter.WriteLineToFile(_replicationFormData.CustomHeader);
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
                            delimitedWriter.AddField(name);
                        }
                    }

                    delimitedWriter.SaveAndCommitLine();
                    // Write data i.e. rows.                    
                    while (dataReader.Read())
                    {
                        foreach (string columnName in columnNames)
                        {
                            delimitedWriter.AddField(
                                dataReader.GetString(
                                    dataReader.GetOrdinal(
                                        columnName))); //dataReader.GetOrdinal(ColumnName) provides the position.
                        }

                        delimitedWriter.SaveAndCommitLine();
                        rowCount++; //Increase row count to track number of rows written.
                    }
                }
            }

            return rowCount;
        }


        public long WriteLineToFile(string filePathAndName, Dictionary<string, object> recordMap,
            bool includeHeader = false, long lineNumber = -1)
        {
            throw new System.NotImplementedException();
        }

        public List<SchemaTable> GetAllTableNames(bool downloadToLocal = false)
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

        public long ImportTable(string filePathAndName, RootPathObject rootPath, bool downloadToLocal = false,
            long limit = -1)
        {
            var delimitedSettings = rootPath.ModeSettings.DelimitedSettings;
            var autoGenRow = delimitedSettings.AutoGenRowNumber;
            var rowsRead = 0;
            var rowsSkipped = 0;
            List<string> headerColumns = new List<string>();

            var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture);
            csvConfiguration.Delimiter = delimitedSettings.GetDelimiter();
            csvConfiguration.HasHeaderRecord = delimitedSettings.HasHeader;
            csvConfiguration.DetectColumnCountChanges = true;

            var streamWrapper = Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode, downloadToLocal);

            using (var csvReader = new CsvReader(streamWrapper.StreamReader, csvConfiguration))
            {
                // skip lines
                if (rootPath.SkipLines > 0)
                {
                    while (csvReader.Read() && rowsSkipped < rootPath.SkipLines)
                    {
                        rowsSkipped++;
                    }
                }

                if (delimitedSettings.HasHeader)
                {
                    csvReader.Read();
                    csvReader.ReadHeader();
                }

                // get column names
                for (var i = 0; i < csvReader.ColumnCount; i++)
                {
                    if (delimitedSettings.HasHeader)
                    {
                        var field = csvReader.HeaderRecord[i];

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
                var querySb =
                    new StringBuilder(
                        $"CREATE TABLE IF NOT EXISTS [{_schemaName}].[{_tableName}] ({(autoGenRow ? $"[{Constants.AutoRowNum}] INTEGER PRIMARY KEY AUTOINCREMENT," : "")}");

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
                    var paramName = $"@param{headerColumns.IndexOf(column)}";
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
                    // csvReader.Configuration.DetectColumnCountChanges = false;
                    // read all lines from file
                    while (csvReader.Read() && rowsRead < limit)
                    {
                        foreach (var column in headerColumns)
                        {
                            var rawValue = csvReader[headerColumns.IndexOf(column)];
                            cmd.Parameters[$"@param{headerColumns.IndexOf(column)}"].Value = rawValue;
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