using Newtonsoft.Json;

namespace Egad
{
    static class JsonWriterExtensions
    {
        public static void WriteProperty(this JsonWriter writer, string propertyName, object propertyValue, JsonSerializer serializer)
        {
            writer.WritePropertyName(propertyName);
            serializer.Serialize(writer, propertyValue);
        }

        public static void WriteProperty(this JsonWriter writer, string propertyName, object propertyValue)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteValue(propertyValue);
        }
    }
}
