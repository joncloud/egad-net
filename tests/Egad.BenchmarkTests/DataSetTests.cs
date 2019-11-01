using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;
using System.Data;
using System.IO;
using System.Text.Json;

namespace Egad.BenchmarkTests
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public abstract class DataSetTests
    {
        readonly DataSetType _type;
        protected DataSetTests(DataSetType type) =>
            _type = type;

        DataSetHarness _harness;
        [GlobalSetup]
        public void Setup()
        {
            _harness = new DataSetHarness(_type);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Write")]
        public Tuple<MemoryStream, MemoryStream> Xml_Write()
        {
            var schema = new MemoryStream();
            var data = new MemoryStream();

            _harness.DataSet.WriteXmlSchema(schema);
            _harness.DataSet.WriteXml(data, XmlWriteMode.DiffGram);

            return Tuple.Create(schema, data);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Read")]
        public DataSet Xml_Read()
        {
            _harness.XmlSchema.Position = 0;
            _harness.XmlData.Position = 0;

            var copy = new DataSet();
            copy.ReadXmlSchema(_harness.XmlSchema);
            copy.ReadXml(_harness.XmlData, XmlReadMode.DiffGram);

            return copy;
        }

        [Benchmark]
        [BenchmarkCategory("Write")]
        public MemoryStream Json_Write()
        {
            var memory = new MemoryStream();
            var writer = new Utf8JsonWriter(memory);

            JsonSerializer.Serialize(writer, _harness.DataSet, _harness.Options);

            writer.Flush();

            return memory;
        }

        //[Benchmark]
        //[BenchmarkCategory("Read")]
        //public DataSet Json_Read()
        //{
        //    _harness.Json.Position = 0;
        //    var reader = new StreamReader(_harness.Json);
        //    Utf8JsonReader
        //    var jsonReader = new JsonTextReader(reader);
        //    return _harness.Options.Deserialize<DataSet>(jsonReader);
        //}
    }
}
