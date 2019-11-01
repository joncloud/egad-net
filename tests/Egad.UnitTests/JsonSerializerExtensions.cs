using Newtonsoft.Json;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Egad.UnitTests
{
    public class JsonSerializerExtensions
    {
        [Fact]
        public void UseEgad_ShouldInsertDataSetConverterAtBeginningOfConverters()
        {
            var serializer = new JsonSerializerOptions();
            var mockConverter = new MockConverter();
            serializer.Converters.Add(mockConverter);
            serializer.UseEgad();
            Assert.IsAssignableFrom<JsonConverter<DataSet>>(serializer.Converters[0]);
            Assert.IsType<MockConverter>(serializer.Converters[1]);
        }
    }
}
