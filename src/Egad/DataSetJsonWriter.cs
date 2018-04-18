using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;

namespace Egad
{
    class DataSetJsonWriter
    {
        readonly JsonSerializer _serializer;
        readonly JsonWriter _writer;
        public DataSetJsonWriter(JsonSerializer serializer, JsonWriter writer)
        {
            _serializer = serializer;
            _writer = writer;
        }

        void WritePropertyIf(string propertyName, bool propertyValue)
        {
            if (!propertyValue) return;

            _writer.WriteProperty(propertyName, propertyValue);
        }

        void WritePropertyIf<T>(string propertyName, T propertyValue, Func<T, bool> condition)
        {
            if (condition(propertyValue))
                _writer.WriteProperty(propertyName, propertyValue);
        }

        void WritePropertyIf<T>(string propertyName, T propertyValue, JsonSerializer serializer, Func<T, bool> condition)
        {
            if (condition(propertyValue))
                _writer.WriteProperty(propertyName, propertyValue, serializer);
        }

        static bool StringIsPopulated(string s) =>
            !string.IsNullOrWhiteSpace(s);

        static bool ArrayHasElements<T>(T[] array) =>
            array.Length > 0;

        public void Write(DataSet dataSet)
        {
            _writer.WriteStartObject();
            _writer.WriteProperty("dataSetName", dataSet.DataSetName);
            WritePropertyIf("enforceConstraints", dataSet.EnforceConstraints);
            WriteProperties(dataSet.ExtendedProperties);
            _writer.WriteProperty("locale", dataSet.Locale, _serializer);
            WritePropertyIf(dataSet.Prefix, dataSet.Prefix, StringIsPopulated);
            WritePropertyIf("caseSensitive", dataSet.CaseSensitive);
            WritePropertyIf("remotingFormat", dataSet.RemotingFormat, x => x == SerializationFormat.Binary);
            WritePropertyIf("schemaSerializationMode", dataSet.SchemaSerializationMode, x => x == SchemaSerializationMode.ExcludeSchema);
            WriteDataTables(dataSet.Tables);
            WritePropertyIf("namespace", dataSet.Namespace, StringIsPopulated);
            WriteDataRelations(dataSet.Relations);
            _writer.WriteEndObject();
        }

        void WriteDataTables(DataTableCollection dataTables)
        {
            if (dataTables.Count == 0) return;

            _writer.WritePropertyName("tables");
            _writer.WriteStartObject();
            foreach (DataTable dataTable in dataTables)
            {
                WriteDataTable(dataTable);
            }
            _writer.WriteEndObject();
        }

        void WriteDataTable(DataTable dataTable)
        {
            _writer.WritePropertyName(dataTable.TableName);
            _writer.WriteStartObject();
            WritePropertyIf("minimumCapacity", dataTable.MinimumCapacity, capacity => capacity != 50);
            _writer.WriteProperty("locale", dataTable.Locale, _serializer);
            WriteProperties(dataTable.ExtendedProperties);
            WritePropertyIf("namespace", dataTable.Namespace, StringIsPopulated);
            //TODO writer.WriteProperty("contraints", value.Constraints, serializer);
            WriteDataColumns(dataTable.Columns);
            WritePropertyIf("displayExpression", dataTable.DisplayExpression, StringIsPopulated);
            WritePropertyIf("remotingFormat", dataTable.RemotingFormat, x => x == SerializationFormat.Binary);
            WritePropertyIf("primaryKey", dataTable.PrimaryKey.Select(col => col.ColumnName).ToArray(), _serializer, ArrayHasElements);
            WritePropertyIf("caseSensitive", dataTable.CaseSensitive);
            WritePropertyIf("prefix", dataTable.Prefix, StringIsPopulated);
            WriteDataRows(dataTable.Rows, dataTable.Columns.Count);
            _writer.WriteEndObject();
        }

        void WriteDataRows(DataRowCollection value, int columnCount)
        {
            if (value.Count == 0) return;

            _writer.WritePropertyName("rows");
            _writer.WriteStartArray();
            foreach (DataRow dataRow in value)
            {
                WriteDataRow(dataRow, columnCount);
            }
            _writer.WriteEndArray();
        }

        void WriteDataRow(DataRow dataRow, int columnCount)
        {
            _writer.WriteStartObject();
            _writer.WriteProperty("rowError", dataRow.RowError);
            _writer.WriteProperty("rowState", dataRow.RowState);
            var state = dataRow.RowState;
            if (state == DataRowState.Deleted || state == DataRowState.Modified)
                WriteCells(dataRow, columnCount, "originalValues", DataRowVersion.Original);
            if (state == DataRowState.Added || state == DataRowState.Modified || state == DataRowState.Unchanged)
                WriteCells(dataRow, columnCount, "currentValues", DataRowVersion.Current);
            _writer.WriteEndObject();
        }

        void WriteCells(DataRow dataRow, int columnCount, string propertyName, DataRowVersion version)
        {
            _writer.WritePropertyName(propertyName);
            _writer.WriteStartArray();
            for (int i = 0; i < columnCount; i++)
            {
                _writer.WriteValue(dataRow[i, version]);
            }
            _writer.WriteEndArray();
        }

        void WriteDataColumns(DataColumnCollection dataColumns)
        {
            if (dataColumns.Count == 0) return;

            _writer.WritePropertyName("columns");
            _writer.WriteStartArray();
            foreach (DataColumn dataColumn in dataColumns)
            {
                WriteDataColumn(dataColumn);
            }
            _writer.WriteEndArray();
        }

        void WriteDataColumn(DataColumn dataColumn)
        {
            _writer.WriteStartObject();
            _writer.WriteProperty("dataType", dataColumn.DataType.FullName);
            _writer.WriteProperty("columnName", dataColumn.ColumnName);

            WritePropertyIf("readOnly", dataColumn.ReadOnly);
            WritePropertyIf("prefix", dataColumn.Prefix, StringIsPopulated);
            WritePropertyIf("namespace", dataColumn.Namespace, StringIsPopulated);
            WritePropertyIf("maxLength", dataColumn.MaxLength, len => len > -1);
            WriteProperties(dataColumn.ExtendedProperties);
            WritePropertyIf("expression", dataColumn.Expression, StringIsPopulated);
            WritePropertyIf("defaultValue", dataColumn.DefaultValue, val => val != DBNull.Value);
            WritePropertyIf("dateTimeMode", dataColumn.DateTimeMode, val => val != DataSetDateTime.UnspecifiedLocal);

            if (dataColumn.AutoIncrement)
            {
                WritePropertyIf("autoIncrementStep", dataColumn.AutoIncrementStep, step => step != 1);
                WritePropertyIf("autoIncrementSeed", dataColumn.AutoIncrementSeed, step => step != 0);
                _writer.WriteProperty("autoIncrement", dataColumn.AutoIncrement);
            }

            WritePropertyIf("caption", dataColumn.Caption, StringIsPopulated);
            WritePropertyIf("allowDbNull", dataColumn.AllowDBNull);
            WritePropertyIf("columnMapping", dataColumn.ColumnMapping, val => val != MappingType.Element);
            WritePropertyIf("unique", dataColumn.Unique);
            _writer.WriteEndObject();
        }

        void WriteProperties(PropertyCollection properties)
        {
            if (properties.Count == 0) return;

            _writer.WritePropertyName("extendedProperties");
            _writer.WriteStartObject();
            foreach (string key in properties.Keys)
            {
                _writer.WriteProperty(key, properties[key]);
            }
            _writer.WriteEndObject();
        }

        void WriteDataRelations(DataRelationCollection dataRelations)
        {
            if (dataRelations.Count == 0) return;

            _writer.WritePropertyName("relations");
            _writer.WriteStartObject();
            foreach (DataRelation dataRelation in dataRelations)
            {
                WriteDataRelation(dataRelation);
            }
            _writer.WriteEndObject();
        }

        void WriteDataRelation(DataRelation dataRelation)
        {
            _writer.WritePropertyName(dataRelation.RelationName);

            _writer.WriteStartObject();

            WriteProperties(dataRelation.ExtendedProperties);
            _writer.WriteProperty("parentColumnNames", dataRelation.ParentColumns.Select(col => col.ColumnName).ToArray(), _serializer);
            _writer.WriteProperty("nested", dataRelation.Nested);
            _writer.WriteProperty("childTableName", dataRelation.ChildTable.TableName);
            _writer.WriteProperty("childColumnNames", dataRelation.ChildColumns.Select(col => col.ColumnName).ToArray(), _serializer);
            _writer.WriteProperty("parentTableName", dataRelation.ParentTable.TableName);
            //WriteUniqueConstraint("parentKeyConstraint", dataRelation.ParentKeyConstraint);
            //WriteForeignKeyConstraint("childKeyConstraint", dataRelation.ChildKeyConstraint);
            _writer.WriteEndObject();
        }

        void WriteUniqueConstraint(string name, UniqueConstraint uniqueConstraint)
        {
            _writer.WritePropertyName(name);
            _writer.WriteStartObject();
            _writer.WriteProperty("columnNames", uniqueConstraint.Columns.Select(col => col.ColumnName).ToArray(), _serializer);
            _writer.WriteProperty("isPrimaryKey", uniqueConstraint.IsPrimaryKey);
            _writer.WriteProperty("tableName", uniqueConstraint.Table.TableName);
            _writer.WriteEndObject();
        }

        void WriteForeignKeyConstraint(string name, ForeignKeyConstraint foreignKeyConstraint)
        {
            _writer.WritePropertyName(name);

            _writer.WriteStartObject();
            _writer.WriteProperty("acceptRejectRule", foreignKeyConstraint.AcceptRejectRule);
            _writer.WriteProperty("columnNames", foreignKeyConstraint.Columns.Select(col => col.ColumnName).ToArray(), _serializer);
            _writer.WriteProperty("deleteRule", foreignKeyConstraint.DeleteRule);
            _writer.WriteProperty("relatedColumnNames", foreignKeyConstraint.RelatedColumns.Select(col => col.ColumnName).ToArray(), _serializer);
            _writer.WriteProperty("relatedTableName", foreignKeyConstraint.RelatedTable.TableName);
            _writer.WriteProperty("tableName", foreignKeyConstraint.Table.TableName);
            _writer.WriteProperty("updateRule", foreignKeyConstraint.UpdateRule);
            _writer.WriteEndObject();
        }

    }
}
