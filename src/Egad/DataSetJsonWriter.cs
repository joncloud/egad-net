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

        public void Write(DataSet dataSet)
        {
            _writer.WriteStartObject();
            _writer.WriteProperty("dataSetName", dataSet.DataSetName);
            _writer.WriteProperty("enforceConstraints", dataSet.EnforceConstraints);
            WriteProperties(dataSet.ExtendedProperties);
            _writer.WriteProperty("locale", dataSet.Locale, _serializer);
            _writer.WriteProperty("prefix", dataSet.Prefix);
            _writer.WriteProperty("caseSensitive", dataSet.CaseSensitive);
            _writer.WriteProperty("remotingFormat", dataSet.RemotingFormat);
            _writer.WriteProperty("schemaSerializationMode", dataSet.SchemaSerializationMode);
            WriteDataTables(dataSet.Tables);
            if (!string.IsNullOrWhiteSpace(dataSet.Namespace))
                _writer.WriteProperty("namespace", dataSet.Namespace);
            WriteDataRelations(dataSet.Relations);
            _writer.WriteEndObject();
        }

        void WriteDataTables(DataTableCollection dataTables)
        {
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
            _writer.WriteProperty("minimumCapacity", dataTable.MinimumCapacity);
            _writer.WriteProperty("locale", dataTable.Locale, _serializer);
            WriteProperties(dataTable.ExtendedProperties);
            if (!string.IsNullOrWhiteSpace(dataTable.Namespace))
                _writer.WriteProperty("namespace", dataTable.Namespace);
            //TODO writer.WriteProperty("contraints", value.Constraints, serializer);
            WriteDataColumns(dataTable.Columns);
            _writer.WriteProperty("displayExpression", dataTable.DisplayExpression);
            _writer.WriteProperty("remotingFormat", dataTable.RemotingFormat);
            _writer.WriteProperty("primaryKey", dataTable.PrimaryKey.Select(col => col.ColumnName).ToArray(), _serializer);
            _writer.WriteProperty("caseSensitive", dataTable.CaseSensitive);
            WriteDataRows(dataTable.Rows);
            _writer.WriteProperty("prefix", dataTable.Prefix);
            _writer.WriteEndObject();
        }

        void WriteDataRows(DataRowCollection value)
        {
            _writer.WritePropertyName("rows");
            _writer.WriteStartArray();
            foreach (DataRow dataRow in value)
            {
                WriteDataRow(dataRow);
            }
            _writer.WriteEndArray();
        }

        void WriteDataRow(DataRow dataRow)
        {
            _writer.WriteStartObject();
            _writer.WriteProperty("rowError", dataRow.RowError);
            _writer.WriteProperty("rowState", dataRow.RowState);
            _writer.WritePropertyName("originalValues");
            WriteCells(DataRowVersion.Original, state => state == DataRowState.Deleted || state == DataRowState.Modified);
            _writer.WritePropertyName("currentValues");
            WriteCells(DataRowVersion.Current, state => state == DataRowState.Added || state == DataRowState.Modified || state == DataRowState.Unchanged);
            _writer.WriteEndObject();

            void WriteCells(DataRowVersion version, Func<DataRowState, bool> fn)
            {
                _writer.WriteStartArray();
                if (fn(dataRow.RowState))
                {
                    for (int i = 0; i < dataRow.Table.Columns.Count; i++)
                    {
                        _writer.WriteValue(dataRow[i, version]);
                    }
                }
                _writer.WriteEndArray();
            }
        }

        void WriteDataColumns(DataColumnCollection dataColumns)
        {
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
            _writer.WriteProperty("readOnly", dataColumn.ReadOnly);
            _writer.WriteProperty("prefix", dataColumn.Prefix);
            if (!string.IsNullOrWhiteSpace(dataColumn.Namespace))
                _writer.WriteProperty("namespace", dataColumn.Namespace);
            _writer.WriteProperty("maxLength", dataColumn.MaxLength);
            _writer.WriteProperty("extendedProperties", dataColumn.ExtendedProperties, _serializer);
            _writer.WriteProperty("expression", dataColumn.Expression);
            _writer.WriteProperty("defaultValue", dataColumn.DefaultValue);
            _writer.WriteProperty("dateTimeMode", dataColumn.DateTimeMode);
            _writer.WriteProperty("dataType", dataColumn.DataType, _serializer);
            _writer.WriteProperty("columnName", dataColumn.ColumnName);
            _writer.WriteProperty("autoIncrementStep", dataColumn.AutoIncrementStep);
            _writer.WriteProperty("caption", dataColumn.Caption);
            _writer.WriteProperty("autoIncrementSeed", dataColumn.AutoIncrementSeed);
            _writer.WriteProperty("autoIncrement", dataColumn.AutoIncrement);
            _writer.WriteProperty("allowDbNull", dataColumn.AllowDBNull);
            _writer.WriteProperty("columnMapping", dataColumn.ColumnMapping);
            _writer.WriteProperty("unique", dataColumn.Unique);
            _writer.WriteEndObject();
        }

        void WriteProperties(PropertyCollection properties)
        {
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

            WriteUniqueConstraint("parentKeyConstraint", dataRelation.ParentKeyConstraint);
            _writer.WriteProperty("parentColumnNames", dataRelation.ParentColumns.Select(col => col.ColumnName).ToArray(), _serializer);
            _writer.WriteProperty("nested", dataRelation.Nested);
            WriteProperties(dataRelation.ExtendedProperties);
            _writer.WriteProperty("childTableName", dataRelation.ChildTable.TableName);
            _writer.WriteProperty("childColumnNames", dataRelation.ChildColumns.Select(col => col.ColumnName).ToArray(), _serializer);
            _writer.WriteProperty("parentTableName", dataRelation.ParentTable.TableName);
            WriteForeignKeyConstraint("childKeyConstraint", dataRelation.ChildKeyConstraint);
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
