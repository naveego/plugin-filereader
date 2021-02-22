using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace PluginFileReader.API.Factory.Implementations.XML
{
    public class XMLImportExport : IImportExportFile
    {
        private readonly SqlDatabaseConnection _conn;
        private readonly RootPathObject _rootPath;
        private readonly string _tableName;
        private readonly string _schemaName;

        public XMLImportExport(SqlDatabaseConnection sqlbaseConnection, RootPathObject rootPath,
            string tableName, string schemaName)
        {
            _conn = sqlbaseConnection;
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
            // variables
            var rowsRead = 0;

            // load xml doc
            XmlDocument doc = new XmlDocument();
            // doc.PreserveWhitespace = true;
            doc.Load(filePathAndName);

            List<XmlNode> removeList = new List<XmlNode>();
            
            foreach (XmlNode node in doc)
            {
                if (
                    node.NodeType == XmlNodeType.XmlDeclaration 
                    || node.NodeType == XmlNodeType.Comment 
                    || node.NodeType == XmlNodeType.ProcessingInstruction
                )
                {
                    removeList.Add(node);
                }
            }

            foreach (var node in removeList)
            {
                doc.RemoveChild(node);
            }

            // convert xml to json
            var json = JsonConvert.SerializeXmlNode(doc);
            var data = JsonHelper.DeserializeAndFlattenToList(json);

            // get column names
            var columns = new List<string>();

            if (data.Count > 0)
            {
                columns = new List<string>(data.First().Keys);
            }

            // setup db table
            var querySb = new StringBuilder($"CREATE TABLE IF NOT EXISTS [{_schemaName}].[{_tableName}] (");

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
            querySb = new StringBuilder($"INSERT INTO [{_schemaName}].[{_tableName}] (");
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

            // read records
            var trans = _conn.BeginTransaction();

            try
            {
                foreach (var item in data)
                {
                    foreach (var column in columns)
                    {
                        var rawValue = item[column]?.ToString();
                        cmd.Parameters[$"@param{columns.IndexOf(column)}"].Value = rawValue;
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

    public static class JsonHelper
    {
        public static List<Dictionary<string, object>> DeserializeAndFlattenToList(string json)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            JToken token = JToken.Parse(json);

            if (token.HasValues)
            {
                var childToken = token.First().First().Value<JToken>();

                if (childToken.HasValues && childToken.Children().Count() == 1)
                {
                    childToken = childToken.First().First().Value<JToken>();
                    
                    if (childToken.Type == JTokenType.Array)
                    {
                        foreach (JToken item in childToken.Children())
                        {
                            Dictionary<string, object> tempDict = new Dictionary<string, object>();
                            FillDictionaryFromJToken(tempDict, item, "");
                            list.Add(tempDict);
                        }
                    
                        return list;
                    }
                }
            }
            
            Dictionary<string, object> dict = new Dictionary<string, object>();
            FillDictionaryFromJToken(dict, token, "");
            list.Add(dict);

            return list;
        }

        private static void FillDictionaryFromJToken(Dictionary<string, object> dict, JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        FillDictionaryFromJToken(dict, prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;

                default:
                    dict.Add(prefix, ((JValue)token).Value);
                    break;
            }
        }


        private static string Join(string prefix, string name)

        {
            return (string.IsNullOrEmpty(prefix) ? name : prefix + "." + name);
        }
    }
}

