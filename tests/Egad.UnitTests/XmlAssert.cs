using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using System.Xml;
using Xunit;

namespace Egad.UnitTests
{
    class XmlAssert
    {
        public static void Matches(DataSet dataSet, Action<DataSet, XmlWriter> fn)
        {
            var cloned = Clone(dataSet);

            var expected = SerializeXml(dataSet, fn);
            var actual = SerializeXml(dataSet, fn);

            Assert.Equal(expected, actual);
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

        static DataSet Clone(DataSet dataSet)
        {
            var settings = new JsonSerializerSettings().UseEgad();
            var json = JsonConvert.SerializeObject(dataSet, settings);
            return JsonConvert.DeserializeObject<DataSet>(json, settings);
        }
    }
}
