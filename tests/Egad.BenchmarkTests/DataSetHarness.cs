using System;
using System.Data;
using System.IO;
using System.Text.Json;

namespace Egad.BenchmarkTests
{
    public class DataSetHarness
    {
        public readonly JsonSerializerOptions Options;
        public readonly DataSet DataSet;
        public readonly MemoryStream XmlSchema;
        public readonly MemoryStream XmlData;
        public readonly MemoryStream Json;

        public DataSetHarness(DataSetType type)
        {
            Options = new JsonSerializerOptions().UseEgad();
            DataSet = CreateDataSet(type);

            XmlSchema = new MemoryStream();
            XmlData = new MemoryStream();

            DataSet.WriteXmlSchema(XmlSchema);
            DataSet.WriteXml(XmlData, XmlWriteMode.DiffGram);

            XmlSchema.Position = 0;
            XmlData.Position = 0;

            Json = new MemoryStream();
            var writer = new Utf8JsonWriter(Json);
            JsonSerializer.Serialize(writer, DataSet, Options);

            writer.Flush();
        }

        static DataTable CreateParentTable()
        {
            var dataTable = new DataTable("Parent");

            dataTable.Columns.Add("Id", typeof(Guid));
            dataTable.Columns.Add("Amount", typeof(Decimal));

            var parentRowId = AddRow(1.111M, _ => { });
            AddRow(2.222M, row => row.AcceptChanges());
            AddRow(3.000M, row => { row.AcceptChanges(); row["Amount"] = 3.333M; });
            AddRow(4.444M, row => { row.AcceptChanges(); row.Delete(); });

            return dataTable;
            
            Guid AddRow(decimal amount, Action<DataRow> fn)
            {
                var id = Guid.NewGuid();
                fn(dataTable.Rows.Add(id, amount));
                return id;
            }
        }

        static DataTable CreateChildTable(Guid parentRowId)
        {
            var table = new DataTable("Child");

            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("ParentId", typeof(Guid));
            table.Columns.Add("Amount", typeof(decimal));

            AddRow(parentRowId, 1.000M, _ => { });
            AddRow(parentRowId, 0.100M, _ => { });
            AddRow(parentRowId, 0.010M, _ => { });
            AddRow(parentRowId, 0.001M, _ => { });

            return table;

            Guid AddRow(Guid parentId, decimal amount, Action<DataRow> fn)
            {
                var id = Guid.NewGuid();
                fn(table.Rows.Add(id, parentId, amount));
                return id;
            }
        }

        static DataTable CreateDataTypesTable()
        {
            var table = new DataTable("DataTypes");

            table.Columns.Add("DateTime", typeof(DateTime));
            table.Columns.Add("int", typeof(int));
            table.Columns.Add("long", typeof(long));
            table.Columns.Add("float", typeof(float));
            table.Columns.Add("double", typeof(double));
            table.Columns.Add("decimal", typeof(decimal));
            table.Columns.Add("Guid", typeof(Guid));
            table.Columns.Add("string", typeof(string));
            table.Columns.Add("nullableInt", typeof(int));

            int count = 50;
            while (--count >= 0)
            {
                table.Rows.Add(
                    DateTime.MaxValue,
                    int.MaxValue,
                    (long)int.MaxValue + 1,
                    float.MaxValue,
                    double.MaxValue,
                    decimal.MaxValue / 10M,
                    Guid.Empty,
                    "lorem ipsum",
                    DBNull.Value
                );
            }

            return table;
        }

        static DataSet CreateDataSet(DataSetType type)
        {
            var dataSet = new DataSet("MyDataSet");

            if (type.HasFlag(DataSetType.Parent))
            {
                var parent = CreateParentTable();
                dataSet.Tables.Add(parent);

                if (type.HasFlag(DataSetType.Child))
                {
                    var child = CreateChildTable((Guid)parent.Rows[0][0]);
                    dataSet.Tables.Add(child);

                    if (type.HasFlag(DataSetType.Relationship))
                    {
                        //dataSet.Relations.Add(
                        //    new DataRelation(
                        //        "A",
                        //        parent.Columns["Id"],
                        //        child.Columns["ParentId"]
                        //    )
                        //);
                    }
                }
            }

            if (type.HasFlag(DataSetType.DataTypes))
            {
                var dataTypes = CreateDataTypesTable();
                dataSet.Tables.Add(dataTypes);
            }

            return dataSet;
        }
    }
}
