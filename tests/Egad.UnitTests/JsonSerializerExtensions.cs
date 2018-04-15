using Newtonsoft.Json;
using System.Data;
using Xunit;

namespace Egad.UnitTests
{
    public class JsonSerializerExtensions
    {
        [Fact]
        public void UseEgad_ShouldInsertDataSetConverterAtBeginningOfConverters()
        {
            var serializer = new JsonSerializer();
            var mockConverter = new MockConverter();
            serializer.Converters.Add(mockConverter);
            serializer.UseEgad();
            Assert.IsAssignableFrom<JsonConverter<DataSet>>(serializer.Converters[0]);
            Assert.IsType<MockConverter>(serializer.Converters[1]);
        }
    }
}
