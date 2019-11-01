using System;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Xml;
using Xunit;

namespace Egad.UnitTests
{
    static class XmlAssert
    {
        public static void Matches(DataSet dataSet, Action<DataSet, XmlWriter> fn)
        {
            var cloned = Json.Clone(dataSet);

            var expected = SerializeXml(dataSet, fn);
            var actual = SerializeXml(cloned, fn);

            Assert.Equal(expected, actual);
        }

        public static void Matches(DataSet expected, DataSet actual, Action<DataSet, XmlWriter> fn)
        {
            var expectedXml = SerializeXml(expected, fn);
            var actualXml = SerializeXml(actual, fn);

            Assert.Equal(expectedXml, actualXml);
        }

        static string SerializeXml(DataSet dataSet, Action<DataSet, XmlWriter> fn)
        {
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter))
            {
                fn(dataSet, xmlWriter);
                return stringWriter.ToString();
            }
        }
    }

    static class Json
    {
        public static T Clone<T>(T value)
        {
            var options = new JsonSerializerOptions().UseEgad();
            var json = JsonSerializer.Serialize(value, options);
            return JsonSerializer.Deserialize<T>(json, options);
        }
    }
}
