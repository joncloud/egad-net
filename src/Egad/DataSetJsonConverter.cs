using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Egad
{
    class DataSetJsonConverter : JsonConverter<DataSet>
    {
        public override DataSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new DataSetJsonReader(options, ref reader).Read();
        }

        public override void Write(Utf8JsonWriter writer, DataSet value, JsonSerializerOptions options)
        {
            new DataSetJsonWriter(options, writer).Write(value);
        }
    }
}
