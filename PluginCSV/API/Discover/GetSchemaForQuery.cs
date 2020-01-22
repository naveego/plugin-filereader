using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PluginCSV.API.Utility;
using PluginCSV.Helper;
using Pub;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginCSV.API.Discover
{
    public static partial class Discover
    {
        public static Schema GetSchemaForQuery(Schema schema, int sampleSize = 5, List<Column> columns = null)
        {
            var conn = Utility.Utility.GetSqlConnection(Constants.DiscoverDbPrefix);

            var cmd = new SqlDatabaseCommand
            {
                Connection = conn,
                CommandText = schema.Query
            };

            var reader = cmd.ExecuteReader();
            var schemaTable = reader.GetSchemaTable();
            
            // var schemaTable = conn.GetSchema("Columns", new string[]
            // {
            //     "[dbo].[ReadDirectory]"
            // });

            if (schemaTable != null)
            {
                var unnamedColIndex = 0;

                // get each column and create a property for the column
                foreach (DataRow row in schemaTable.Rows)
                {
                    // get the column name
                    var colName = row["ColumnName"].ToString();
                    if (string.IsNullOrWhiteSpace(colName))
                    {
                        colName = $"UNKNOWN_{unnamedColIndex}";
                        unnamedColIndex++;
                    }

                    // create property
                    Property property;
                    if (columns == null)
                    {
                        property = new Property
                        {
                            Id = colName,
                            Name = colName,
                            Description = "",
                            Type = GetPropertyType(row),
                            TypeAtSource = row["DataType"].ToString(),
                            IsKey = Boolean.Parse(row["IsKey"].ToString()),
                            IsNullable = Boolean.Parse(row["AllowDBNull"].ToString()),
                            IsCreateCounter = false,
                            IsUpdateCounter = false,
                            PublisherMetaJson = ""
                        };
                    }
                    else
                    {
                        var column = columns.FirstOrDefault(c => c.ColumnName == colName);
                        property = new Property
                        {
                            Id = colName,
                            Name = colName,
                            Description = "",
                            Type = GetPropertyType(row),
                            TypeAtSource = row["DataType"].ToString(),
                            IsKey = column?.IsKey ?? Boolean.Parse(row["IsKey"].ToString()),
                            IsNullable = !column?.IsKey ?? Boolean.Parse(row["AllowDBNull"].ToString()),
                            IsCreateCounter = false,
                            IsUpdateCounter = false,
                            PublisherMetaJson = ""
                        };
                    }
                    
                    // add property to schema
                    schema.Properties.Add(property);
                }
            }

            var records = Read.Read.ReadRecords(schema, Constants.DiscoverDbPrefix).Take(sampleSize);
            schema.Sample.AddRange(records);

            schema.Count = Read.Read.GetCountOfRecords(schema, Constants.DiscoverDbPrefix);

            return schema;
        }
    }
}