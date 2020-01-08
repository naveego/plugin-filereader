using System;
using System.Data;
using Pub;

namespace PluginCSV.API.Discover
{
    public static partial class Discover
    {
        private static PropertyType GetPropertyType(DataColumn column)
        {
            var type = column.DataType;
            switch (true)
            {
                case bool _ when type == typeof(bool):
                    return PropertyType.Bool;
                case bool _ when type == typeof(int):
                case bool _ when type == typeof(long):
                    return PropertyType.Integer;
                case bool _ when type == typeof(float):
                case bool _ when type == typeof(double):
                    return PropertyType.Float;
                case bool _ when type == typeof(DateTime):
                    return PropertyType.Datetime;
                case bool _ when type == typeof(string):
                    if (column.MaxLength > 1024)
                    {
                        return PropertyType.Text;
                    }

                    return PropertyType.String;
                default:
                    return PropertyType.String;
            }
        }
    }
}