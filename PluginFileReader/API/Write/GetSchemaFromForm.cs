using System.Collections.Generic;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginFileReader.DataContracts;

namespace PluginFileReader.API.Write
{
    public static partial class Write
    {
        public static Schema GetSchemaFromForm(ConfigureWriteFormData formData)
        {
            var schema = new Schema
            {
                Id = "",
                Name = "",
                DataFlowDirection = Schema.Types.DataFlowDirection.Write,
                PublisherMetaJson = JsonConvert.SerializeObject(formData),
            };

            var properties = new List<Property>();
            foreach (var column in formData.Columns)
            {
                properties.Add(new Property
                {
                    // Id = $"{column.Name}{(string.IsNullOrWhiteSpace(column.DefaultValue) ? "" : $"_{column.DefaultValue}")}",
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