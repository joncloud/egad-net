using System;
using System.Data;
using Xunit;

namespace Egad.UnitTests
{

    public class DataSetTests
    {
        [Fact]
        public void SchemaShouldMatch()
        {
            var dataSet = CreateDataSet();

            XmlAssert.Matches(dataSet, (ds, writer) => ds.WriteXmlSchema(writer));
        }

        [Fact]
        public void DiffGramShouldMatch()
        {
            var dataSet = CreateDataSet();

            XmlAssert.Matches(dataSet, (ds, writer) => ds.WriteXml(writer, XmlWriteMode.DiffGram));
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
