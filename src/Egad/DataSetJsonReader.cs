using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Egad
{
    class DataSetJsonReader
    {
        readonly JsonSerializer _serializer;
        public DataSetJsonReader(JsonSerializer serializer) => _serializer = serializer;

        public DataSet Read(JsonReader reader)
        {
            var jobject = _serializer.Deserialize<JObject>(reader);

            var dataSet = new DataSet();
            dataSet.DataSetName = jobject.GetPropertyValue<string>("dataSetName");
            dataSet.EnforceConstraints = jobject.GetPropertyValue<bool>("enforceConstraints");
            PopulateProperties(dataSet.ExtendedProperties, (JObject)jobject.Property("extendedProperties").Value);
            dataSet.Locale = jobject.GetPropertyValue<CultureInfo>("locale", _serializer);
            dataSet.Prefix = jobject.GetPropertyValue<string>("prefix");
            dataSet.CaseSensitive = jobject.GetPropertyValue<bool>("caseSensitive");
            dataSet.RemotingFormat = jobject.GetPropertyValue<SerializationFormat>("remotingFormat", _serializer);
            dataSet.SchemaSerializationMode = jobject.GetPropertyValue<SchemaSerializationMode>("schemaSerializationMode", _serializer);
            PopulateDataTables(dataSet.Tables, (JObject)jobject.Property("tables").Value);
            dataSet.Namespace = jobject.GetPropertyValue<string>("namespace");


            //jobject.PopulateWithPropertyValue("relations", serializer, dataSet.Relations);
            return dataSet;
        }

        void PopulateDataRelations(DataSet dataSet, JObject jobject)
        {
            foreach (var jproperty in jobject.Properties())
            {
                var dataRelation = ReadDataRelation(
                    jproperty.Name,
                    (JObject)jproperty.Value
                );
                dataSet.Relations.Add(dataRelation);
            }
        }

        DataRelation ReadDataRelation(string relationName, JObject jobject)
        {
            var dataRelation = new DataRelation(
                relationName,
                jobject.GetPropertyValue<string>("parentTableName"),
                jobject.GetPropertyValue<string>("childTableName"),
                jobject.GetPropertyValue<string[]>("parentColumnNames", _serializer),
                jobject.GetPropertyValue<string[]>("childColumnNames", _serializer),
                jobject.GetPropertyValue<bool>("nested")
            );

            // ParentKeyConstraint?
            // ChildKeyConstraint?

            PopulateProperties(dataRelation.ExtendedProperties, (JObject)jobject.Property("extendedProperties").Value);

            return dataRelation;
        }

        void PopulateDataTables(DataTableCollection dataTables, JObject jobject)
        {
            foreach (var jproperty in jobject.Properties())
            {
                DataTable dataTable = dataTables.Add(jproperty.Name);

                PopulateDataTable(dataTable, (JObject)jproperty.Value);
            }
        }

        void PopulateDataTable(DataTable dataTable, JObject jobject)
        {
            dataTable.MinimumCapacity = jobject.GetPropertyValue<int>("minimumCapacity");
            dataTable.Locale = jobject.GetPropertyValue<CultureInfo>("locale", _serializer);
            PopulateProperties(dataTable.ExtendedProperties, (JObject)jobject.Property("extendedProperties").Value);
            dataTable.Namespace = jobject.GetPropertyValue<string>("namespace");
            PopulateDataColumns(dataTable.Columns, (JArray)jobject.Property("columns").Value);
            dataTable.DisplayExpression = jobject.GetPropertyValue<string>("displayExpression");
            dataTable.RemotingFormat = jobject.GetPropertyValue<SerializationFormat>("remotingFormat", _serializer);
            dataTable.PrimaryKey = jobject.GetPropertyValue<string[]>("primaryKey", _serializer).Select(name => dataTable.Columns[name]).ToArray();
            dataTable.CaseSensitive = jobject.GetPropertyValue<bool>("caseSensitive");
            PopulateDataRows(dataTable, (JArray)jobject.Property("rows").Value);
            dataTable.TableName = jobject.GetPropertyValue<string>("tableName");
            dataTable.Prefix = jobject.GetPropertyValue<string>("prefix");
        }

        void PopulateDataRows(DataTable dataTable, JArray jarray)
        {
            int columnCount = dataTable.Columns.Count;
            foreach (var jobject in jarray.OfType<JObject>())
            {
                DataRow row = dataTable.NewRow();
                dataTable.Rows.Add(row);
                row.RowError = jobject.GetPropertyValue<string>("rowError");
                var rowState = jobject.GetPropertyValue<DataRowState>("rowState", _serializer);
                switch (rowState)
                {
                    case DataRowState.Added:
                        SetRowValues("currentValues");
                        break;
                    case DataRowState.Deleted:
                        SetRowValues("originalValues");
                        row.AcceptChanges();
                        row.Delete();
                        break;
                    case DataRowState.Modified:
                        SetRowValues("originalValues");
                        row.AcceptChanges();
                        SetRowValues("currentValues");
                        break;
                    default:
                        SetRowValues("currentValues");
                        row.AcceptChanges();
                        break;
                }

                void SetRowValues(string propertyName)
                {
                    var rowValues = jobject.Property(propertyName).Value.ToObject<object[]>(_serializer);

                    for (int i = 0; i < columnCount; i++)
                    {
                        row[i] = rowValues[i];
                    }
                }
            }
        }

        void PopulateDataColumns(DataColumnCollection dataColumns, JArray jarray)
        {
            dataColumns.AddRange(jarray.OfType<JObject>().Select(ReadDataColumn).ToArray());
        }

        DataColumn ReadDataColumn(JObject jobject)
        {
            var dataColumn = new DataColumn();
            dataColumn.ReadOnly = jobject.GetPropertyValue<bool>("readOnly");
            dataColumn.Prefix = jobject.GetPropertyValue<string>("prefix");
            dataColumn.Namespace = jobject.GetPropertyValue<string>("namespace");
            dataColumn.MaxLength = jobject.GetPropertyValue<int>("maxLength");
            PopulateProperties(dataColumn.ExtendedProperties, (JObject)jobject.Property("extendedProperties").Value);
            dataColumn.Expression = jobject.GetPropertyValue<string>("expression");
            dataColumn.DataType = jobject.GetPropertyValue<Type>("dataType", _serializer);
            dataColumn.DefaultValue = jobject.GetPropertyValue("defaultValue", _serializer, dataColumn.DataType);
            dataColumn.DateTimeMode = jobject.GetPropertyValue<DataSetDateTime>("dateTimeMode", _serializer);
            dataColumn.ColumnName = jobject.GetPropertyValue<string>("columnName");
            dataColumn.AutoIncrementStep = jobject.GetPropertyValue<long>("autoIncrementStep");
            dataColumn.Caption = jobject.GetPropertyValue<string>("caption");
            dataColumn.AutoIncrementSeed = jobject.GetPropertyValue<long>("autoIncrementSeed");
            dataColumn.AutoIncrement = jobject.GetPropertyValue<bool>("autoIncrement");
            dataColumn.AllowDBNull = jobject.GetPropertyValue<bool>("allowDbNull");
            dataColumn.ColumnMapping = jobject.GetPropertyValue<MappingType>("columnMapping", _serializer);
            dataColumn.Unique = jobject.GetPropertyValue<bool>("unique");
            return dataColumn;
        }

        void PopulateProperties(PropertyCollection properties, JObject jobject)
        {
            foreach (var jproperty in jobject.Properties())
            {
                properties.Add(jproperty.Name, 0);
            }
        }
    }
}
