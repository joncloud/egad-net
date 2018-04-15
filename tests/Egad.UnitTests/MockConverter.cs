using Newtonsoft.Json;
using System;

namespace Egad.UnitTests
{
    class MockConverter : JsonConverter<Mock>
    {
        public override Mock ReadJson(JsonReader reader, Type objectType, Mock existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Mock value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
