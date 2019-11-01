using System;
using System.Text.Json;

namespace Egad
{
    static class Utf8JsonWriterExtensions
    {
        public static void WriteObject(this Utf8JsonWriter writer, string propertyName, object value, Type type, JsonSerializerOptions options)
        {
            writer.WritePropertyName(propertyName);
            if (value == DBNull.Value)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, value, type, options);
            }
        }

        public static void WriteObjectValue(this Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options)
        {
            if (value == DBNull.Value)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, value, type, options);
            }
        }
    }
}
