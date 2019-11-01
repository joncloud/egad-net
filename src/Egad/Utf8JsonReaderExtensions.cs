using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Egad
{
    static class Utf8JsonReaderExtensions
    {
        delegate object GetObjectDelegate(ref Utf8JsonReader reader);

        static Dictionary<RuntimeTypeHandle, GetObjectDelegate> _getObject =
            new Dictionary<RuntimeTypeHandle, GetObjectDelegate>
            {
                [typeof(string).TypeHandle] = GetString,
                [typeof(DateTime).TypeHandle] = GetDateTime,
                [typeof(bool).TypeHandle] = GetBoolean,
                [typeof(int).TypeHandle] = GetInt32,
                [typeof(long).TypeHandle] = GetInt64,
                [typeof(decimal).TypeHandle] = GetDecimal,
                [typeof(float).TypeHandle] = GetSingle,
                [typeof(Guid).TypeHandle] = GetGuid,
                [typeof(double).TypeHandle] = GetDouble
            };

        static object GetString(ref Utf8JsonReader reader) => reader.GetString();
        static object GetDateTime(ref Utf8JsonReader reader) => reader.GetDateTime();
        static object GetBoolean(ref Utf8JsonReader reader) => reader.GetBoolean();
        static object GetInt32(ref Utf8JsonReader reader) => reader.GetInt32();
        static object GetInt64(ref Utf8JsonReader reader) => reader.GetInt64();
        static object GetDecimal(ref Utf8JsonReader reader) => reader.GetDecimal();
        static object GetSingle(ref Utf8JsonReader reader) => reader.GetSingle();
        static object GetGuid(ref Utf8JsonReader reader) => reader.GetGuid();
        static object GetDouble(ref Utf8JsonReader reader) => reader.GetDouble();

        public static object GetObject(this ref Utf8JsonReader reader, Type type)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            else if (_getObject.TryGetValue(type.TypeHandle, out var fn))
            {
                return fn(ref reader);
            }
            else
            {
                return reader.GetString();
            }
        }
    }
}
