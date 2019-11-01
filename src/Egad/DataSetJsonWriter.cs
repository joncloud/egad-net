using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace Egad
{
    readonly ref struct DataSetJsonWriter
    {
        readonly JsonSerializerOptions _options;
        readonly Utf8JsonWriter _writer;
        public DataSetJsonWriter(JsonSerializerOptions options, Utf8JsonWriter writer)
        {
            _options = options;
            _writer = writer;
        }

        void WritePropertyIf(string propertyName, bool propertyValue)
        {
            if (!propertyValue) return;

            _writer.WriteBoolean(propertyName, propertyValue);
        }

        void WritePropertyIf(string propertyName, string propertyValue, Func<string, bool> condition)
        {
            if (condition(propertyValue))
                _writer.WriteString(propertyName, propertyValue);
        }

        void WritePropertyIf(string propertyName, IEnumerable<string> propertyValue, Func<IEnumerable<string>, bool> condition)
        {
            if (condition(propertyValue))
            {
                _writer.WritePropertyName(propertyName);
                _writer.WriteStartArray();
                foreach (var value in propertyValue)
                {
                    _writer.WriteStringValue(value);
                }
                _writer.WriteEndArray();
            }
        }

        void WritePropertyIf(string propertyName, int propertyValue, Func<int, bool> condition)
        {
            if (condition(propertyValue))
                _writer.WriteNumber(propertyName, propertyValue);
        }

        void WritePropertyIf(string propertyName, long propertyValue, Func<long, bool> condition)
        {
            if (condition(propertyValue))
                _writer.WriteNumber(propertyName, propertyValue);
        }

        static bool StringIsPopulated(string s) =>
            !string.IsNullOrWhiteSpace(s);

        static bool ArrayHasElements<T>(IEnumerable<T> source) =>
            source.Any();

        public void Write(DataSet dataSet)
        {
            _writer.WriteStartObject();
            _writer.WriteString("dataSetName", dataSet.DataSetName);
            WritePropertyIf("enforceConstraints", dataSet.EnforceConstraints);
            WriteProperties(dataSet.ExtendedProperties);
            _writer.WriteString("locale", dataSet.Locale.ToString());
            WritePropertyIf(dataSet.Prefix, dataSet.Prefix, StringIsPopulated);
            WritePropertyIf("caseSensitive", dataSet.CaseSensitive);
            WritePropertyIf("remotingFormat", (int)dataSet.RemotingFormat, x => x == (int)SerializationFormat.Binary);
            WritePropertyIf("schemaSerializationMode", (int)dataSet.SchemaSerializationMode, x => x == (int)SchemaSerializationMode.ExcludeSchema);
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
            _writer.WriteString("locale", dataTable.Locale.ToString());
            WriteProperties(dataTable.ExtendedProperties);
            WritePropertyIf("namespace", dataTable.Namespace, StringIsPopulated);
            //TODO writer.WriteProperty("contraints", value.Constraints, serializer);
            WriteDataColumns(dataTable.Columns);
            WritePropertyIf("displayExpression", dataTable.DisplayExpression, StringIsPopulated);
            WritePropertyIf("remotingFormat", (int)dataTable.RemotingFormat, x => x == (int)SerializationFormat.Binary);
            WritePropertyIf("primaryKey", dataTable.PrimaryKey.Select(col => col.ColumnName), ArrayHasElements);
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
            _writer.WriteString("rowError", dataRow.RowError);
            _writer.WriteNumber("rowState", (int)dataRow.RowState);
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
                var type = dataRow.Table.Columns[i].DataType;
                _writer.WriteObjectValue(dataRow[i, version], type, _options);
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
            _writer.WriteString("dataType", dataColumn.DataType.FullName);
            _writer.WriteString("columnName", dataColumn.ColumnName);

            WritePropertyIf("readOnly", dataColumn.ReadOnly);
            WritePropertyIf("prefix", dataColumn.Prefix, StringIsPopulated);
            WritePropertyIf("namespace", dataColumn.Namespace, StringIsPopulated);
            WritePropertyIf("maxLength", dataColumn.MaxLength, len => len > -1);
            WriteProperties(dataColumn.ExtendedProperties);
            WritePropertyIf("expression", dataColumn.Expression, StringIsPopulated);
            // TODO WritePropertyIf("defaultValue", dataColumn.DefaultValue, val => val != DBNull.Value);
            WritePropertyIf("dateTimeMode", (int)dataColumn.DateTimeMode, val => val != (int)DataSetDateTime.UnspecifiedLocal);

            if (dataColumn.AutoIncrement)
            {
                WritePropertyIf("autoIncrementStep", dataColumn.AutoIncrementStep, step => step != 1);
                WritePropertyIf("autoIncrementSeed", dataColumn.AutoIncrementSeed, step => step != 0);
                _writer.WriteBoolean("autoIncrement", dataColumn.AutoIncrement);
            }

            WritePropertyIf("caption", dataColumn.Caption, StringIsPopulated);
            WritePropertyIf("allowDbNull", dataColumn.AllowDBNull);
            WritePropertyIf("columnMapping", (int)dataColumn.ColumnMapping, val => val != (int)MappingType.Element);
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
                var value = properties[key];

                _writer.WritePropertyName(key);

                if (value == null)
                {
                    _writer.WriteNullValue();
                }
                else
                {
                    var type = value.GetType();
                    _writer.WriteStartObject();
                    _writer.WriteString("type", type.FullName);
                    _writer.WriteObject("value", value, type, _options);
                    _writer.WriteEndObject();
                }
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
            WritePropertyIf("parentColumnNames", dataRelation.ParentColumns.Select(col => col.ColumnName), ArrayHasElements);
            _writer.WriteBoolean("nested", dataRelation.Nested);
            _writer.WriteString("childTableName", dataRelation.ChildTable.TableName);
            WritePropertyIf("childColumnNames", dataRelation.ChildColumns.Select(col => col.ColumnName), ArrayHasElements);
            _writer.WriteString("parentTableName", dataRelation.ParentTable.TableName);
            //WriteUniqueConstraint("parentKeyConstraint", dataRelation.ParentKeyConstraint);
            //WriteForeignKeyConstraint("childKeyConstraint", dataRelation.ChildKeyConstraint);
            _writer.WriteEndObject();
        }

        void WriteUniqueConstraint(string name, UniqueConstraint uniqueConstraint)
        {
            _writer.WritePropertyName(name);
            _writer.WriteStartObject();
            WritePropertyIf("columnNames", uniqueConstraint.Columns.Select(col => col.ColumnName), ArrayHasElements);
            _writer.WriteBoolean("isPrimaryKey", uniqueConstraint.IsPrimaryKey);
            _writer.WriteString("tableName", uniqueConstraint.Table.TableName);
            _writer.WriteEndObject();
        }

        void WriteForeignKeyConstraint(string name, ForeignKeyConstraint foreignKeyConstraint)
        {
            _writer.WritePropertyName(name);

            _writer.WriteStartObject();
            _writer.WriteNumber("acceptRejectRule", (int)foreignKeyConstraint.AcceptRejectRule);
            WritePropertyIf("columnNames", foreignKeyConstraint.Columns.Select(col => col.ColumnName), ArrayHasElements);
            _writer.WriteNumber("deleteRule", (int)foreignKeyConstraint.DeleteRule);
            WritePropertyIf("relatedColumnNames", foreignKeyConstraint.RelatedColumns.Select(col => col.ColumnName), ArrayHasElements);
            _writer.WriteString("relatedTableName", foreignKeyConstraint.RelatedTable.TableName);
            _writer.WriteString("tableName", foreignKeyConstraint.Table.TableName);
            _writer.WriteNumber("updateRule", (int)foreignKeyConstraint.UpdateRule);
            _writer.WriteEndObject();
        }

    }
}
