using System.IO;
using System.Text;
using PluginFileReader.API.Factory;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.FixedWidthColumns
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

        public int ExportTable(string filePathAndName, bool appendToFile = false)
        {
            throw new System.NotImplementedException();
        }

        public long ImportTable(string filePathAndName, RootPathObject rootPath, long limit = long.MaxValue)
        {
            // setup db table
            var querySb = new StringBuilder($"CREATE TABLE IF NOT EXISTS [{_schemaName}].[{_tableName}] (");
            var primaryKeySb = new StringBuilder("PRIMARY KEY (");
            var hasPrimaryKey = false;
            foreach (var column in rootPath.Columns)
            {
                querySb.Append(
                    $"[{column.ColumnName}] VARCHAR({int.MaxValue}){(column.IsKey ? " NOT NULL UNIQUE" : "")},");
                if (column.IsKey)
                {
                    primaryKeySb.Append($"{column.ColumnName},");
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

            var cmd = new SqlDatabaseCommand
            {
                Connection = _conn,
                CommandText = query
            };

            cmd.ExecuteNonQuery();

            // read file into db
            var file = new StreamReader(filePathAndName);
            string line;
            var rowsRead = 0;

            // prepare insert cmd with parameters
            querySb = new StringBuilder($"INSERT INTO [{_schemaName}].[{_tableName}] (");
            foreach (var column in rootPath.Columns)
            {
                querySb.Append($"[{column.ColumnName}],");
            }

            querySb.Length--;
            querySb.Append(") VALUES (");

            foreach (var column in rootPath.Columns)
            {
                var paramName = $"@{column.ColumnName}";
                querySb.Append($"{paramName},");
                cmd.Parameters.Add(paramName);
            }

            querySb.Length--;
            querySb.Append(");");

            query = querySb.ToString();

            cmd.CommandText = query;

            // read all lines from file
            while ((line = file.ReadLine()) != null && rowsRead < limit)
            {
                foreach (var column in rootPath.Columns)
                {
                    var rawValue = line.Substring(column.ColumnStart, column.ColumnEnd - column.ColumnStart + 1);
                    cmd.Parameters[$"@{column.ColumnName}"].Value = column.TrimWhitespace ? rawValue.Trim() : rawValue;
                }

                cmd.ExecuteNonQuery();

                rowsRead++;
            }

            return rowsRead;
        }
    }
}