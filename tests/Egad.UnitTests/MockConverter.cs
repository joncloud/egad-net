using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Egad.UnitTests
{
    class MockConverter : JsonConverter<Mock>
    {
        public override Mock Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Mock value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
