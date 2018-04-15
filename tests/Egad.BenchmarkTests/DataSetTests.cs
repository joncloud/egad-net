using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;

namespace Egad.BenchmarkTests
{
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

        [Benchmark]
        public Tuple<MemoryStream, MemoryStream> Xml_Write()
        {
            var schema = new MemoryStream();
            var data = new MemoryStream();

            _harness.DataSet.WriteXmlSchema(schema);
            _harness.DataSet.WriteXml(data, XmlWriteMode.DiffGram);

            return Tuple.Create(schema, data);
        }

        [Benchmark]
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
        public MemoryStream Json_Write()
        {
            var memory = new MemoryStream();
            var writer = new StreamWriter(memory);

            _harness.JsonSerializer.Serialize(writer, _harness.DataSet);

            writer.Flush();

            return memory;
        }

        [Benchmark]
        public DataSet Json_Read()
        {
            _harness.Json.Position = 0;
            var reader = new StreamReader(_harness.Json);
            var jsonReader = new JsonTextReader(reader);
            return _harness.JsonSerializer.Deserialize<DataSet>(jsonReader);
        }
    }
}
