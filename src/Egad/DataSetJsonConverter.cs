using Newtonsoft.Json;
using System;
using System.Data;

namespace Egad
{
    class DataSetJsonConverter : JsonConverter<DataSet>
    {
        public override DataSet ReadJson(JsonReader reader, Type objectType, DataSet existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new DataSetJsonReader(serializer).Read(reader);
        }

        public override void WriteJson(JsonWriter writer, DataSet value, JsonSerializer serializer)
        {
            new DataSetJsonWriter(serializer, writer).Write(value);
        }
    }
}
