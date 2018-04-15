using Newtonsoft.Json;

namespace Egad
{
    public static class JsonSerializerExtensions
    {
        public static JsonSerializer UseEgad(this JsonSerializer serializer)
        {
            serializer.Converters.Insert(0, new DataSetJsonConverter());
            return serializer;
        }
    }
}
