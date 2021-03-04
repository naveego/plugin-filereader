using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using PluginFileReader.API.Utility;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.XML
{
    public class XmlImportExport : IImportExportFile
    {
        private readonly SqlDatabaseConnection _conn;
        private readonly RootPathObject _rootPath;
        private readonly string _tableName;
        private readonly string _schemaName;

        public XmlImportExport(SqlDatabaseConnection sqlbaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            _conn = sqlbaseConnection;
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
            throw new System.NotImplementedException();
        }
        
        public List<SchemaTable> GetAllTableNames(string filePathAndName)
        {
            var tableNamesList = new List<SchemaTable>();

            if (string.IsNullOrWhiteSpace(filePathAndName))
            {
                return tableNamesList;
            }
            
            // parse xml into multiple tables
            DataSet dataSet = new DataSet();

            var stream = Utility.Utility.GetStream(_rootPath.ModeSettings.XMLSettings.XsdFilePathAndName,
                _rootPath.FileReadMode);
            dataSet.ReadXmlSchema(stream.Stream);
            stream.Close();

            // create and load each table
            foreach (DataTable table in dataSet.Tables)
            {
                // create table
                tableNamesList.Add(new SchemaTable
                {
                    SchemaName = _schemaName,
                    TableName = $"{_tableName}_{table.TableName}"
                });
            }

            return tableNamesList;
        }

        public long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = -1)
        {
            var totalRowsRead = 0;
            
            // get the global key
            var globalKeyId = "GLOBAL_KEY";
            var globalKeyIndexId = "GLOBAL_KEY_INDEX";
            var globalKeySb = new StringBuilder();
            var globalKeyValue = "";
            
            // load xml doc
            var stream = Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode);
            XmlDocument doc = new XmlDocument();
            doc.Load(stream.Stream);

            foreach (var xmlKey in rootPath.ModeSettings.XMLSettings.XmlKeys)
            {
                var elements = doc.GetElementsByTagName(xmlKey.ElementId);

                XmlNode element;
                try
                {
                    element = elements[0];
                }
                catch (Exception e)
                {
                    throw new Exception($"Element {xmlKey.ElementId} is defined as a key and the element does not exist in the file {Path.GetFileName(filePathAndName)}");
                }

                if (!string.IsNullOrWhiteSpace(xmlKey.AttributeId))
                {
                    if (element.Attributes?[xmlKey.AttributeId] == null)
                    {
                        throw new Exception($"Attribute {xmlKey.AttributeId} is defined as a key on element {xmlKey.ElementId} and the attribute is null or not exist in the file {Path.GetFileName(filePathAndName)}");
                    }
                    
                    var attribute = element.Attributes[xmlKey.AttributeId];

                    if (string.IsNullOrWhiteSpace(attribute.Value))
                    {
                        throw new Exception($"Attribute {xmlKey.AttributeId} is defined as a key on element {xmlKey.ElementId} and the attribute is null or not exist in the file {Path.GetFileName(filePathAndName)}");
                    }

                    globalKeySb.Append(attribute.Value);
                }
                else
                {
                    var elementValue = element.Value;
                    
                    if (string.IsNullOrWhiteSpace(elementValue))
                    {
                        throw new Exception($"Element {xmlKey.ElementId} is defined as a key and the element value is null in the file {Path.GetFileName(filePathAndName)}");
                    }

                    globalKeySb.Append(elementValue);
                }
            }

            globalKeyValue = globalKeySb.ToString();
            
            stream.Close();
            
            // parse xml into multiple tables
            DataSet dataSet = new DataSet();

            stream = Utility.Utility.GetStream(rootPath.ModeSettings.XMLSettings.XsdFilePathAndName,
                rootPath.FileReadMode);
            dataSet.ReadXmlSchema(stream.Stream);
            stream.Close();

            dataSet.Locale = CultureInfo.InvariantCulture;
            dataSet.EnforceConstraints = false;

            stream = Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode);
            dataSet.ReadXml(stream.Stream, XmlReadMode.Auto);
            stream.Close();

            // create and load each table
            foreach (DataTable table in dataSet.Tables)
            {
                var rowsRead = 0;
                
                // create table
                var fullTableName = $"{_tableName}_{table.TableName}";
                var columns = new List<string>();
                
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    columns.Add(table.Columns[i].ColumnName);
                }
                
                // append global key and global key index columns
                columns.Add(globalKeyId);
                columns.Add(globalKeyIndexId);
                
                // setup db table
                var querySb = new StringBuilder($"CREATE TABLE IF NOT EXISTS [{_schemaName}].[{fullTableName}] (");
                
                // add the columns
                foreach (var column in columns)
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
                querySb = new StringBuilder($"INSERT INTO [{_schemaName}].[{fullTableName}] (");
                foreach (var column in columns)
                {
                    querySb.Append($"[{column}],");
                }
                
                querySb.Length--;
                querySb.Append(") VALUES (");
                
                foreach (var column in columns)
                {
                    var paramName = $"@param{columns.IndexOf(column)}";
                    querySb.Append($"{paramName},");
                    cmd.Parameters.Add(paramName);
                }
                
                querySb.Length--;
                querySb.Append(");");
                
                query = querySb.ToString();
                
                Logger.Debug($"Insert record query: {query}");
                
                cmd.CommandText = query;
                
                // load data
                var trans = _conn.BeginTransaction();
                
                try
                {
                    foreach (var row in table.AsEnumerable())
                    {
                        var rowIndex = table.AsEnumerable().ToList().IndexOf(row);

                        foreach (var column in columns)
                        {
                            var columnIndex = columns.IndexOf(column);

                            if (column == globalKeyId)
                            {
                                cmd.Parameters[$"@param{columnIndex}"].Value = globalKeyValue;
                            }
                            else if (column == globalKeyIndexId)
                            {
                                cmd.Parameters[$"@param{columnIndex}"].Value = $"{globalKeyValue}_{rowIndex}";
                            }
                            else
                            {
                                var rawValue = row[columnIndex]?.ToString();
                                cmd.Parameters[$"@param{columnIndex}"].Value = rawValue;
                            }
                        }
                        
                        cmd.ExecuteNonQuery();
                
                        rowsRead++;
                
                        // commit every 1000 rows
                        if (rowsRead % 1000 == 0)
                        {
                            trans.Commit();
                            trans = _conn.BeginTransaction();
                        }
                
                        if (rowsRead > limit)
                        {
                            break;
                        }
                    }
                    
                    // commit any pending inserts
                    trans.Commit();
                    totalRowsRead += rowsRead;
                }
                catch (Exception e)
                {
                    // rollback on error
                    trans.Rollback();
                    Logger.Error(e, e.Message);
                    throw;
                }
            }

            return totalRowsRead;
        }
    }
}

