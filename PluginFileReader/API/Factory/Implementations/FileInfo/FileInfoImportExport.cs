using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Naveego.Sdk.Logging;
using Naveego.Sdk.Plugins;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Factory.Implementations.FileInfo
{
    public class FileInfoImportExport : IImportExportFile
    {
        private readonly SqlDatabaseConnection _conn;
        private readonly RootPathObject _rootPath;
        private readonly string _tableName;
        private readonly string _schemaName;

        public FileInfoImportExport(SqlDatabaseConnection sqlDatabaseConnection, RootPathObject rootPath,
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

        public long WriteLineToFile(string filePathAndName, Dictionary<string, object> recordMap,
            bool includeHeader = false, long lineNumber = -1)
        {
            throw new NotImplementedException();
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
            long limit = long.MaxValue)
        {
            var createAndInsert = string.IsNullOrWhiteSpace(filePathAndName) || rootPath == null;

            var rootPathName = "";
            var fileName = filePathAndName;
            var fileType = "";
            var fileSize = "";

            // get information from the file path and name
            Logger.Debug("Getting file information...");

            if (rootPath != null) rootPathName = rootPath.RootPath;

            if (!string.IsNullOrWhiteSpace(filePathAndName))
            {
                try
                {
                    fileName = Path.GetFileName(filePathAndName);
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message);
                }

                try
                {
                    fileType = Path.GetExtension(filePathAndName).Trim();
                    if (!string.IsNullOrWhiteSpace(fileType))
                    {
                        if (fileType.StartsWith('.'))
                            fileType = $"{fileType.ToUpper().Substring(1)} file";
                        else if (fileType == ".")
                            fileType = null;
                        else
                            fileType = $"{fileType.ToUpper()} file";
                    }
                    else fileType = null;
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message);
                }
            }

            // build target table from static file info properties
            var queryBuilder = new StringBuilder($"CREATE TABLE IF NOT EXISTS [{_schemaName}].[{_tableName}] (");
            var keyCols = new List<Property>();
            foreach (var prop in FileInfoData.FileInfoProperties)
            {
                queryBuilder.Append($"\n[{prop.Id}] {prop.TypeAtSource},");
                if (!prop.IsNullable) queryBuilder.Insert(queryBuilder.Length - 1, " NOT NULL");
                if (prop.IsKey) keyCols.Add(prop);
            }
            queryBuilder.Append("\nPRIMARY KEY (");
            foreach (var prop in keyCols)
            {
                queryBuilder.Append($"{prop.Id}, ");
            }
            queryBuilder.Remove(queryBuilder.Length - 2, 2); // remove trailing comma and space
            queryBuilder.Append(")\n);");

            var query = queryBuilder.ToString();
            queryBuilder.Clear();

            Logger.Debug($"Create table query: {query}");

            var cmd = new SqlDatabaseCommand
            {
                Connection = _conn,
                CommandText = query
            };

            cmd.ExecuteNonQuery();

            if (!createAndInsert)
            {
                // get file size
                var streamWrapper =
                    Utility.Utility.GetStream(filePathAndName, rootPath.FileReadMode, true);
                
                try
                {
                    fileSize = streamWrapper.PrintStreamLength();
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message);
                }
                
                // close input file stream
                streamWrapper.Close();

                // build insert query
                queryBuilder.Append($"INSERT INTO [{_schemaName}].[{_tableName}] (");
                foreach (var prop in FileInfoData.FileInfoProperties)
                {
                    queryBuilder.Append($"\n[{prop.Id}],");
                }
                queryBuilder.Length--; // remove trailing comma
                queryBuilder.Append("\n) VALUES (");
                for (var i = 0; i < FileInfoData.FileInfoProperties.Count; i++)
                {
                    queryBuilder.Append($"\n@param{i},");
                }
                queryBuilder.Length--; // remove trailing comma
                queryBuilder.Append("\n);");

                query = queryBuilder.ToString();

                Logger.Debug($"Insert record query: {query}");

                cmd.CommandText = query;

                var trans = _conn.BeginTransaction();

                try
                {
                    // set params
                    // Root Path Name
                    cmd.Parameters.Add("@param0");
                    cmd.Parameters[$"@param0"].Value = rootPathName;
                    // File Name
                    cmd.Parameters.Add("@param1");
                    cmd.Parameters[$"@param1"].Value = fileName;
                    // File Type (extension)
                    cmd.Parameters.Add("@param2");
                    cmd.Parameters[$"@param2"].Value = fileType;
                    // File Size
                    cmd.Parameters.Add("@param3");
                    cmd.Parameters[$"@param3"].Value = fileSize;
                    
                    cmd.ExecuteNonQuery();
                    
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

            return createAndInsert ? 0 : 1;
        }
    }
}