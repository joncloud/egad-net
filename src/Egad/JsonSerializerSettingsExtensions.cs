using Newtonsoft.Json;

namespace Egad
{
    public static class JsonSerializerSettingsExtensions
    {
        public static JsonSerializerSettings UseEgad(this JsonSerializerSettings settings)
        {
            settings.Converters.Insert(0, new DataSetJsonConverter());
            return settings;
        }
    }
}
