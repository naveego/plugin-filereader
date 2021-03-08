using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.API.Utility;
using PluginFileReader.DataContracts;
using PluginFileReader.Helper;
using SQLDatabase.Net.SQLDatabaseClient;

namespace PluginFileReader.API.Discover
{
    public static partial class Discover
    {
        public static Schema GetSchemaForQuery(Schema schema, int sampleSize = 5, List<Column> columns = null)
        {
            try
            {
                Logger.Debug(JsonConvert.SerializeObject(schema, Formatting.Indented));

                var query = schema.Query;
                
                if (schema.DataFlowDirection == Schema.Types.DataFlowDirection.Write)
                {
                    Logger.Info("Returning Write schema unchanged");
                    return schema;
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    if (!string.IsNullOrWhiteSpace(schema.PublisherMetaJson))
                    {
                        JsonConvert.DeserializeObject<ConfigureWriteFormData>(schema.PublisherMetaJson);
                        Logger.Info("Returning Write schema with direction changed");
                        schema.DataFlowDirection = Schema.Types.DataFlowDirection.Write;
                        return schema;
                    }

                    query = Utility.Utility.GetDefaultQuery(schema);

                    // Logger.Info("Returning null schema for null query");
                    // return null;
                }

                var conn = Utility.Utility.GetSqlConnection(Constants.DiscoverDbPrefix);

                var cmd = new SqlDatabaseCommand
                {
                    Connection = conn,
                    CommandText = query
                };

                var reader = cmd.ExecuteReader();
                var schemaTable = reader.GetSchemaTable();

                var properties = new List<Property>();
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

                        // add property to properties
                        properties.Add(property);
                    }
                }
                
                // add only discovered properties to schema
                schema.Properties.Clear();
                schema.Properties.AddRange(properties);

                var records = Read.Read.ReadRecords(schema, Constants.DiscoverDbPrefix).Take(sampleSize);
                schema.Sample.AddRange(records);
                
                // purge publisher meta json
                schema.PublisherMetaJson = null;

                return schema;
            }
            catch (Exception e)
            {
                // return schema that existed before but files may not currently exist
                if (schema.Properties.Count > 0)
                {
                    return schema;
                }

                Logger.Error(e, e.Message);
                throw;
            }
        }
    }
}