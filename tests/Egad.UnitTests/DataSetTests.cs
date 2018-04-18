using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using Xunit;

namespace Egad.UnitTests
{
    public class DataSetTests
    {
        [PlatformFact]
        public void GivenNetPlatformBoundary_DiffGramShouldMatch()
        {
            var original = CreateDataSet();
            var copy = NetPlatformTester.Passthrough(original);

            XmlAssert.Matches(original, copy, DiffGramTest);
        }

        [PlatformFact]
        public void GivenNetPlatformBoundary_DataSetShouldDeserialize()
        {
            var dataSet = NetPlatformTester.Generate();
            Assert.NotNull(dataSet);
        }

        class Wrapper
        {
            public string A { get; set; }
            public List<int> B { get; set; }
            public DataSet DataSet { get; set; }
            public List<int> Y { get; set; }
            public string Z { get; set; }
        }

        [Fact]
        public void GivenDataSetArray_DiffGramShouldMatch()
        {
            var array = new[]
            {
                CreateDataSet(),
                CreateDataSet(),
                CreateDataSet()
            };

            var copy = Json.Clone(array);

            Assert.NotNull(copy);
            Assert.Equal(array.Length, copy.Length);

            for (int i = 0; i < array.Length; i++)
            {
                XmlAssert.Matches(array[i], copy[i], DiffGramTest);
            }
        }

        [Fact]
        public void GivenWrappedDataSet_DiffGramShouldMatch()
        {
            var wrapper = new Wrapper
            {
                A = "A",
                B = new List<int> { 1, 2, 3 },
                DataSet = CreateDataSet(),
                Y = new List<int> { 4, 5, 6 },
                Z = "Z"
            };

            var copy = Json.Clone(wrapper);

            Assert.NotNull(copy);
            Assert.Equal(wrapper.A, copy.A);
            Assert.Equal(wrapper.B, copy.B);
            Assert.Equal(wrapper.Y, copy.Y);
            Assert.Equal(wrapper.Z, copy.Z);

            XmlAssert.Matches(wrapper.DataSet, copy.DataSet, DiffGramTest);
        }

        [Fact]
        public void GivenSingleDataSet_SchemaShouldMatch()
        {
            var dataSet = CreateDataSet();

            // TODO handle constraints and relationships.
            foreach (DataTable dataTable in dataSet.Tables)
            {
                dataTable.Constraints.Clear();
            }
            dataSet.Relations.Clear();

            XmlAssert.Matches(dataSet, (ds, writer) => ds.WriteXmlSchema(writer));
        }

        [Fact]
        public void GivenSingleDataSet_DiffGramShouldMatch()
        {
            var dataSet = CreateDataSet();
            XmlAssert.Matches(dataSet, DiffGramTest);
        }

        static void DiffGramTest(DataSet dataSet, XmlWriter xmlWriter)
        {
            dataSet.WriteXml(xmlWriter, XmlWriteMode.DiffGram);

        }
        static DataSet CreateDataSet()
        {
            var dataSet = new DataSet("MyDataSet");
            var parent = dataSet.Tables.Add("Parent");

            parent.Columns.Add("Id", typeof(Guid));
            parent.Columns.Add("Amount", typeof(Decimal));

            var parentRowId = AddParentRow(1.111M, _ => { });
            AddParentRow(2.222M, row => row.AcceptChanges());
            AddParentRow(3M, row => { row.AcceptChanges(); row["Amount"] = 3.333M; });
            AddParentRow(4.444M, row => { row.AcceptChanges(); row.Delete(); });

            var child = dataSet.Tables.Add("Child");

            child.Columns.Add("Id", typeof(Guid));
            child.Columns.Add("ParentId", typeof(Guid));
            child.Columns.Add("Amount", typeof(decimal));

            AddChildRow(parentRowId, 1M, _ => { });
            AddChildRow(parentRowId, 0.1M, _ => { });
            AddChildRow(parentRowId, 0.01M, _ => { });
            AddChildRow(parentRowId, 0.001M, _ => { });

            dataSet.Relations.Add(
                new DataRelation(
                    "A",
                    parent.Columns["Id"],
                    child.Columns["ParentId"]
                )
            );

            var dataTypes = dataSet.Tables.Add("DataTypes");

            dataTypes.Columns.Add("DateTime", typeof(DateTime));
            dataTypes.Columns.Add("int", typeof(int));
            dataTypes.Columns.Add("long", typeof(long));
            dataTypes.Columns.Add("float", typeof(float));
            dataTypes.Columns.Add("double", typeof(double));
            dataTypes.Columns.Add("decimal", typeof(decimal));
            dataTypes.Columns.Add("Guid", typeof(Guid));
            dataTypes.Columns.Add("string", typeof(string));
            dataTypes.Columns.Add("nullableInt", typeof(int));

            var random = new Random(0);
            dataTypes.Rows.Add(
                DateTime.UtcNow.AddDays(random.Next(10)),
                random.Next(),
                ((long)random.Next()) << 10,
                (float)random.NextDouble(),
                random.NextDouble(),
                (decimal)random.NextDouble(),
                Guid.NewGuid(),
                Guid.NewGuid().ToString(),
                DBNull.Value
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
