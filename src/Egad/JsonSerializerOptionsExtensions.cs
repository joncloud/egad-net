using System.Text.Json;

namespace Egad
{
    public static class JsonSerializerOptionsExtensions
    {
        public static JsonSerializerOptions UseEgad(this JsonSerializerOptions options)
        {
            options.Converters.Insert(0, new DataSetJsonConverter());
            return options;
        }
    }
}
