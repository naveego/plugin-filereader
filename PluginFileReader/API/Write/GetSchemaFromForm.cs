using System;
using System.Collections.Generic;
using System.IO;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.DataContracts;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        public static Schema GetSchemaFromForm(ConfigureWriteFormData formData)
        {
            var tableName = new DirectoryInfo(formData.TargetFileName).Name;
            
            var schema = new Schema
            {
                Id = tableName,
                Name = tableName,
                DataFlowDirection = Schema.Types.DataFlowDirection.Write,
                Query = tableName,
                PublisherMetaJson = JsonConvert.SerializeObject(formData),
            };

            var properties = new List<Property>();
            foreach (var column in formData.Columns)
            {
                if (properties.Exists(p => p.Id == column.Name))
                {
                    throw new Exception($"Duplicate column {column.Name} defined.");
                }
                
                properties.Add(new Property
                {
                    Id = column.Name,
                    Name = column.Name,
                    Type = PropertyType.String,
                    PublisherMetaJson = JsonConvert.SerializeObject(column)
                });
            }

            schema.Properties.AddRange(properties);
            
            return schema;
        }
    }
}