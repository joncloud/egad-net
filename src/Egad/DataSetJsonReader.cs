using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Egad
{
    class DataSetJsonReader
    {
        readonly JsonSerializer _serializer;
        readonly JsonReader _reader;
        public DataSetJsonReader(JsonSerializer serializer, JsonReader reader)
        {
             _serializer = serializer;
             _reader = reader;
        }

        interface IDataSetLexer 
        {
            void Lex(JsonReader reader);
        }

        abstract class LexerBase : IDataSetLexer
        {
            protected abstract IDataSetLexer HandleProperty(string propertyName, JsonReader reader);

            protected virtual bool ContinueReading(int depth) => true;

            public void Lex(JsonReader reader)
            {
                var lexers = new Stack<IDataSetLexer>();
                var lastPropertyName = default(string);
                var reading = true;

                do
                {
                    if (lexers.TryPop(out var lexer))
                    {
                        lexer.Lex(reader);
                    }
                    else
                    {
                        switch (reader.TokenType)
                        {
                            default:
                                if (lastPropertyName != null)
                                {
                                    lexer = HandleProperty(lastPropertyName, reader);
                                    if (lexer == null)
                                    {
                                        reading = reader.Read();
                                    }
                                    else
                                    {
                                        lexers.Push(lexer);
                                    }
                                    lastPropertyName = null;
                                }
                                else
                                {
                                    reading = reader.Read();
                                }
                                break;

                            case JsonToken.PropertyName:
                                lastPropertyName = (string)reader.Value;
                                reading = reader.Read();
                                break; 
                        }
                    }
                } while (reading && ContinueReading(lexers.Count));
            }
        }

        class DataSetLexer : LexerBase
        {
            readonly JsonSerializer _serializer;
            public DataSet DataSet { get; }
            public DataSetLexer(JsonSerializer serializer) 
            {
                _serializer = serializer;
                DataSet = new DataSet();
            }

            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader)
            {
                switch (propertyName)
                {
                    case "dataSetName":
                        DataSet.DataSetName = (string)reader.Value;
                        break;
                    case "enforceConstraints":
                        DataSet.EnforceConstraints = (bool)reader.Value;
                        break;
                    // case "locale":
                    //     DataSet.Locale = _serializer.Deserialize<CultureInfo>(reader);
                    //     break;
                    case "prefix":
                        DataSet.Prefix = (string)reader.Value;
                        break;
                    case "caseSensitive":
                        DataSet.CaseSensitive = (bool)reader.Value;
                        break;
                    case "remotingFormat":
                        DataSet.RemotingFormat = _serializer.Deserialize<SerializationFormat>(reader);
                        break;
                    case "schemaSerializationMode":
                        DataSet.SchemaSerializationMode = _serializer.Deserialize<SchemaSerializationMode>(reader);
                        break;
                    case "namespace":
                        DataSet.Namespace = (string)reader.Value;
                        break;
                    case "extendedProperties":
                        return new PropertyCollectionLexer(_serializer, DataSet.ExtendedProperties);
                    case "tables":
                        return new DataTableCollectionLexer(_serializer, DataSet.Tables);
                }

                return null;
            }
        }

        class DataTableCollectionLexer : LexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataTableCollection _dataTables;

            public DataTableCollectionLexer(JsonSerializer serializer, DataTableCollection dataTables)
            {
                _serializer = serializer;
                _dataTables = dataTables;
            }

            protected override bool ContinueReading(int depth) => depth > 0;

            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader)
            {
                var dataTable = _dataTables.Add(propertyName);
                return new DataTableLexer(_serializer, dataTable);
            }
        }

        class DataTableLexer : LexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataTable _dataTable;

            public DataTableLexer(JsonSerializer serializer, DataTable dataTable)
            {
                _serializer = serializer;
                _dataTable = dataTable;
            }

            protected override bool ContinueReading(int depth) => depth > 0;
            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader)
            {
                switch (propertyName)
                {
                    case "minimumCapacity":
                        _dataTable.MinimumCapacity = (int)reader.Value;
                        break;
                    // case "locale":
                    //     _dataTable.Locale = _serializer.Deserialize<CultureInfo>(reader);
                    //     break;
                    case "extendedProperties":
                        return new PropertyCollectionLexer(_serializer, _dataTable.ExtendedProperties);
                    case "namespace":
                        _dataTable.Namespace = (string)reader.Value;
                        break;
                    case "columns":
                        // return new DataColumnCollectionLexer(_serializer, _dataTable.Columns);
                        break;
                    case "displayExpression":
                        _dataTable.DisplayExpression = (string)reader.Value;
                        break;
                    case "remotingFormat":
                        _dataTable.RemotingFormat = _serializer.Deserialize<SerializationFormat>(reader);
                        break;
                    case "primaryKey":
                        // TODO ensure occurs after columns are set.
                        _dataTable.PrimaryKey = _serializer.Deserialize<string[]>(reader).Select(name => _dataTable.Columns[name]).ToArray();
                        break;
                    case "caseSensitive":
                        _dataTable.CaseSensitive = (bool)reader.Value;
                        break;
                    case "rows":
                        // return new DataRowCollectionLexer(_serializer, _dataTable);
                        break;
                    case "tableName":
                        _dataTable.TableName = (string)reader.Value;
                        break;
                    case "prefix":
                        _dataTable.Prefix = (string)reader.Value;
                        break;
                }
                return null;
            }
        }

        class PropertyCollectionLexer : IDataSetLexer
        {
            readonly JsonSerializer _serializer;
            readonly PropertyCollection _properties;

            public PropertyCollectionLexer(JsonSerializer serializer, PropertyCollection properties)
            {
                _serializer = serializer;
                _properties = properties;
            }

            public void Lex(JsonReader reader)
            {
                var depth = 0;
                var lastPropertyName = default(string);
                do
                {
                    if (lastPropertyName == null)
                    {
                        switch (reader.TokenType)
                        {
                            case JsonToken.PropertyName:
                                lastPropertyName = (string)reader.Value;
                                break;
                            case JsonToken.StartObject:
                                depth++;
                                break;
                            case JsonToken.EndObject:
                                depth--;
                                break;
                        }
                    }
                    else
                    {
                        var value = _serializer.Deserialize(reader);
                        _properties.Add(lastPropertyName, value);
                        lastPropertyName = null;
                    }

                } while (depth > 0 && reader.Read());
            }
        }

        public DataSet Read()
        {
            if (_reader.Read())
            {
                var lexer = new DataSetLexer(_serializer);
                lexer.Lex(_reader);
                return lexer.DataSet;
            }
            return null;
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

            //PopulateProperties(dataRelation.ExtendedProperties, (JObject)jobject.Property("extendedProperties").Value);

            return dataRelation;
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
                        row[i] = rowValues[i] ?? DBNull.Value;
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
            //PopulateProperties(dataColumn.ExtendedProperties, (JObject)jobject.Property("extendedProperties").Value);
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
    }
}
