using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;

namespace Egad.BenchmarkTests
{
    [MemoryDiagnoser]
    public class Tests
    {
        [Benchmark]
        public Tuple<MemoryStream, MemoryStream> Xml_Write()
        {
            var schema = new MemoryStream();
            var data = new MemoryStream();

            _dataSet.WriteXmlSchema(schema);
            _dataSet.WriteXml(data, XmlWriteMode.DiffGram);

            return Tuple.Create(schema, data);
        }

        [Benchmark]
        public DataSet Xml_Read()
        {
            _xmlSchema.Position = 0;
            _xmlData.Position = 0;

            var copy = new DataSet();
            copy.ReadXmlSchema(_xmlSchema);
            copy.ReadXml(_xmlData, XmlReadMode.DiffGram);

            return copy;
        }

        [Benchmark]
        public MemoryStream Json_Write()
        {
            var memory = new MemoryStream();
            var writer = new StreamWriter(memory);

            _serializer.Serialize(writer, _dataSet);

            writer.Flush();

            return memory;
        }

        [Benchmark]
        public DataSet Json_Read()
        {
            _json.Position = 0;
            var reader = new StreamReader(_json);
            var jsonReader = new JsonTextReader(reader);
            return _serializer.Deserialize<DataSet>(jsonReader);
        }

        static readonly JsonSerializer _serializer;
        static readonly DataSet _dataSet;
        static readonly MemoryStream _xmlSchema;
        static readonly MemoryStream _xmlData;
        static readonly MemoryStream _json;
        static Tests()
        {
            _serializer = new JsonSerializer().UseEgad();
            _dataSet = CreateDataSet();

            _xmlSchema = new MemoryStream();
            _xmlData = new MemoryStream();

            _dataSet.WriteXmlSchema(_xmlSchema);
            _dataSet.WriteXml(_xmlData, XmlWriteMode.DiffGram);

            _xmlSchema.Position = 0;
            _xmlData.Position = 0;

            _json = new MemoryStream();
            var writer = new StreamWriter(_json);

            _serializer.Serialize(writer, _dataSet);

            writer.Flush();
        }

        static DataSet CreateDataSet()
        {
            var dataSet = new DataSet("MyDataSet");
            var parent = dataSet.Tables.Add("Parent");

            parent.Columns.Add("Id", typeof(Guid));
            parent.Columns.Add("Amount", typeof(Decimal));

            var parentRowId = AddParentRow(1.111M, _ => { });
            AddParentRow(2.222M, row => row.AcceptChanges());
            AddParentRow(3.000M, row => { row.AcceptChanges(); row["Amount"] = 3.333M; });
            AddParentRow(4.444M, row => { row.AcceptChanges(); row.Delete(); });

            var child = dataSet.Tables.Add("Child");

            child.Columns.Add("Id", typeof(Guid));
            child.Columns.Add("ParentId", typeof(Guid));
            child.Columns.Add("Amount", typeof(decimal));

            AddChildRow(parentRowId, 1.000M, _ => { });
            AddChildRow(parentRowId, 0.100M, _ => { });
            AddChildRow(parentRowId, 0.010M, _ => { });
            AddChildRow(parentRowId, 0.001M, _ => { });

            var dataTypes = dataSet.Tables.Add("DataTypes");

            dataTypes.Columns.Add("DateTime", typeof(DateTime));
            dataTypes.Columns.Add("int", typeof(int));
            dataTypes.Columns.Add("long", typeof(long));
            dataTypes.Columns.Add("float", typeof(float));
            dataTypes.Columns.Add("double", typeof(double));
            dataTypes.Columns.Add("decimal", typeof(decimal));
            dataTypes.Columns.Add("Guid", typeof(Guid));
            dataTypes.Columns.Add("string", typeof(string));

            var random = new Random(0);
            dataTypes.Rows.Add(
                DateTime.UtcNow.AddDays(random.Next(10)),
                random.Next(),
                ((long)random.Next()) << 10,
                (float)random.NextDouble(),
                random.NextDouble(),
                (decimal)random.NextDouble(),
                Guid.NewGuid(),
                Guid.NewGuid().ToString()
            );

            Guid AddParentRow(decimal amount, Action<DataRow> fn)
            {
                var id = Guid.NewGuid();
                fn(parent.Rows.Add(id, amount));
                return id;
            }

            Guid AddChildRow(Guid parentId, decimal amount, Action<DataRow> fn)
            {
                var id = Guid.NewGuid();
                fn(child.Rows.Add(id, parentId, amount));
                return id;
            }

            return dataSet;
        }
    }
}
