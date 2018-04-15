using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Egad
{
    static class JObjectExtensions
    {
        public static T GetPropertyValue<T>(this JObject jobject, string name)
        {
            return jobject.Property(name).Value.Value<T>();
        }

        public static T GetPropertyValue<T>(this JObject jobject, string name, JsonSerializer serializer)
        {
            var value = jobject.Property(name).Value;
            return value.Type == JTokenType.Null ? default(T) : value.ToObject<T>(serializer);
        }

        public static object GetPropertyValue(this JObject jobject, string name, JsonSerializer serializer, Type type)
        {
            var value = jobject.Property(name).Value;
            return value.Type == JTokenType.Null ? null : value.ToObject(type, serializer);
        }
    }
}
