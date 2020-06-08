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
                    Id = $"{column.Name}_{column.DefaultValue}",
                    Name = column.Name,
                    Type = PropertyType.String
                });
            }

            schema.Properties.AddRange(properties);
            
            return schema;
        }
    }
}