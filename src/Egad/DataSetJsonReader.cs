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

        abstract class ArrayLexerBase : IDataSetLexer
        {
            protected abstract IDataSetLexer HandleObject(JsonReader reader);

            protected virtual bool ContinueReading(int depth) => depth > 0;

            public void Lex(JsonReader reader)
            {
                var lexers = new Stack<IDataSetLexer>();
                var reading = true;
                var depth = 0;

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
                            case JsonToken.StartArray:
                                depth++;
                                reading = reader.Read();
                                break;
                            case JsonToken.EndArray:
                                depth--;
                                reading = reader.Read();
                                break;

                            case JsonToken.StartObject:
                                lexer = HandleObject(reader);
                                if (lexer == null)
                                {
                                    reading = reader.Read();
                                }
                                else
                                {
                                    lexers.Push(lexer);
                                }
                                break;
                        }
                    }
                } while (reading && ContinueReading(depth));
            }
        }

        abstract class ObjectLexerBase : IDataSetLexer
        {
            protected abstract IDataSetLexer HandleProperty(string propertyName, JsonReader reader);

            protected virtual bool ContinueReading(int depth) => true;

            public void Lex(JsonReader reader)
            {
                var lexers = new Stack<IDataSetLexer>();
                var lastPropertyName = default(string);
                var reading = true;
                var depth = 0;

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
                                if (reader.TokenType == JsonToken.StartObject) depth++;
                                else if (reader.TokenType == JsonToken.EndObject) depth--;
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
                } while (reading && ContinueReading(depth));
            }
        }

        class DataSetLexer : ObjectLexerBase
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
                    case "locale":
                        var locale = _serializer.Deserialize<CultureInfo>(reader);
                        if (!CultureInfo.CurrentCulture.Equals(locale))
                        {
                            DataSet.Locale = locale;
                        }
                        break;
                    case "prefix":
                        DataSet.Prefix = (string)reader.Value;
                        break;
                    case "caseSensitive":
                        if ((bool)reader.Value)
                            DataSet.CaseSensitive = true;
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
                    case "relations":
                        return new DataRelationCollectionLexer(_serializer, DataSet.Relations);
                }

                return null;
            }
        }

        class DataRelationCollectionLexer : ObjectLexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataRelationCollection _relations;

            public DataRelationCollectionLexer(JsonSerializer serializer, DataRelationCollection relations)
            {
                _serializer = serializer;
                _relations = relations;
            }

            protected override bool ContinueReading(int depth) => depth > 0;

            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader) =>
                new DataRelationLexer(_serializer, _relations, propertyName);
        }

        class DataRelationLexer : ObjectLexerBase
        {
            readonly string _name;
            readonly DataRelationCollection _relations;
            readonly JsonSerializer _serializer;
            public DataRelationLexer(JsonSerializer serializer, DataRelationCollection relations, string name)
            {
                _serializer = serializer;
                _relations = relations;
                _name = name;
            }

            protected override bool ContinueReading(int depth) => depth > 0;

            string[] _parentColumnNames;
            bool _nested;
            string _childTableName;
            string[] _childColumnNames;
            string _parentTableName;

            int _propertyReadCount = 0;
            bool _added;
            void AddIfReached()
            {
                if (_added) return;
                if (++_propertyReadCount < 5) return;
                _relations.Add(
                    new DataRelation(
                        _name,
                        _parentTableName,
                        _childTableName,
                        _parentColumnNames,
                        _childColumnNames,
                        _nested
                    )
                );
                _added = true;
            }
            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader)
            {
                switch (propertyName)
                {
                    case "parentColumnNames":
                        _parentColumnNames = _serializer.Deserialize<string[]>(reader);
                        AddIfReached();
                        break;
                    case "nested":
                        _nested = (bool)reader.Value;
                        AddIfReached();
                        break;
                    case "childTableName":
                        _childTableName = (string)reader.Value;
                        AddIfReached();
                        break;
                    case "childColumnNames":
                        _childColumnNames = _serializer.Deserialize<string[]>(reader);
                        AddIfReached();
                        break;
                    case "parentTableName":
                        _parentTableName = (string)reader.Value;
                        AddIfReached();
                        break;
                }
                return null;
            }
        }

        class DataTableCollectionLexer : ObjectLexerBase
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

        class DataTableLexer : ObjectLexerBase
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
                        _dataTable.MinimumCapacity = (int)(long)reader.Value;
                        break;
                    case "locale":
                        var locale = _serializer.Deserialize<CultureInfo>(reader);
                        if (!CultureInfo.CurrentCulture.Equals(locale))
                        {
                            _dataTable.Locale = locale;
                        }
                        break;
                    case "extendedProperties":
                        return new PropertyCollectionLexer(_serializer, _dataTable.ExtendedProperties);
                    case "namespace":
                        _dataTable.Namespace = (string)reader.Value;
                        break;
                    case "columns":
                        return new DataColumnCollectionLexer(_serializer, _dataTable.Columns);
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
                        if ((bool)reader.Value)
                            _dataTable.CaseSensitive = true;
                        break;
                    case "rows":
                        return new DataRowCollectionLexer(_serializer, _dataTable);
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

        class DataRowCollectionLexer : ArrayLexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataTable _dataTable;

            public DataRowCollectionLexer(JsonSerializer serializer, DataTable dataTable)
            {
                _serializer = serializer;
                _dataTable = dataTable;
            }

            protected override IDataSetLexer HandleObject(JsonReader reader)
            {
                var dataRow = _dataTable.NewRow();
                _dataTable.Rows.Add(dataRow);
                return new DataRowLexer(_serializer, dataRow, _dataTable.Columns.Count);
            }
        }

        class DataRowLexer : ObjectLexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataRow _dataRow;
            readonly int _columnCount;

            public DataRowLexer(JsonSerializer serializer, DataRow dataRow, int columnCount)
            {
                _serializer = serializer;
                _dataRow = dataRow;
                _columnCount = columnCount;
            }

            protected override bool ContinueReading(int depth) => depth > 0;

            DataRowState _rowState;
            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader)
            {
                switch (propertyName)
                {
                    case "rowError":
                        _dataRow.RowError = (string)reader.Value;
                        break;
                    case "rowState":
                        _rowState = _serializer.Deserialize<DataRowState>(reader);
                        break;
                    case "currentValues":
                        object[] currentValues = _serializer.Deserialize<object[]>(reader);
                        switch (_rowState)
                        {
                            case DataRowState.Added:
                                SetRowValues(currentValues);
                                break;
                            case DataRowState.Modified:
                                SetRowValues(currentValues);
                                break;
                            default:
                                SetRowValues(currentValues);
                                _dataRow.AcceptChanges();
                                break;

                            case DataRowState.Deleted:
                                break;
                        }
                        break;
                    case "originalValues":
                        object[] originalValues = _serializer.Deserialize<object[]>(reader);
                        switch (_rowState)
                        {
                            case DataRowState.Deleted:
                                SetRowValues(originalValues);
                                _dataRow.AcceptChanges();
                                _dataRow.Delete();
                                break;
                            case DataRowState.Modified:
                                SetRowValues(originalValues);
                                _dataRow.AcceptChanges();
                                break;
                        }
                        break;
                }
                return null;

                void SetRowValues(object[] rowValues)
                {
                    for (int i = 0; i < _columnCount; i++)
                    {
                        _dataRow[i] = rowValues[i] ?? DBNull.Value;
                    }
                }
            }
        }

        class DataColumnCollectionLexer : ArrayLexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataColumnCollection _dataColumns;
            public DataColumnCollectionLexer(JsonSerializer serializer, DataColumnCollection dataColumns)
            {
                _serializer = serializer;
                _dataColumns = dataColumns;
            }

            protected override IDataSetLexer HandleObject(JsonReader reader)
            {
                var dataColumn = new DataColumn();
                _dataColumns.Add(dataColumn);
                return new DataColumnLexer(_serializer, dataColumn);
            }
        }

        class DataColumnLexer : ObjectLexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataColumn _dataColumn;
            public DataColumnLexer(JsonSerializer serializer, DataColumn dataColumn)
            {
                _serializer = serializer;
                _dataColumn = dataColumn;
            }

            protected override bool ContinueReading(int depth) => depth > 0;

            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader)
            {
                switch (propertyName)
                {
                    case "readOnly":
                        _dataColumn.ReadOnly = (bool)reader.Value;
                        break;
                    case "prefix":
                        _dataColumn.Prefix = (string)reader.Value;
                        break;
                    case "namespace":
                        _dataColumn.Namespace = (string)reader.Value;
                        break;
                    case "maxLength":
                        _dataColumn.MaxLength = (int)(long)reader.Value;
                        break;
                    case "extendedProperties":
                        return new PropertyCollectionLexer(_serializer, _dataColumn.ExtendedProperties);
                    case "expression":
                        _dataColumn.Expression = (string)reader.Value;
                        break;
                    case "dataType":
                        _dataColumn.DataType = _serializer.Deserialize<Type>(reader);
                        break;
                    case "defaultValue":
                        if (reader.TokenType == JsonToken.Null)
                            _dataColumn.DefaultValue = DBNull.Value;

                        else
                        {
                            // TODO ensure called after datatype
                            _dataColumn.DefaultValue = _serializer.Deserialize(reader, _dataColumn.DataType);
                        }
                        break;
                    case "dateTimeMode":
                        _dataColumn.DateTimeMode = _serializer.Deserialize<DataSetDateTime>(reader);
                        break;
                    case "columnName":
                        _dataColumn.ColumnName = (string)reader.Value;
                        break;
                    case "autoIncrementStep":
                        _dataColumn.AutoIncrementStep = (long)reader.Value;
                        break;
                    case "caption":
                        _dataColumn.Caption = (string)reader.Value;
                        break;
                    case "autoIncrementSeed":
                        _dataColumn.AutoIncrementSeed = (long)reader.Value;
                        break;
                    case "autoIncrement":
                        _dataColumn.AutoIncrement = (bool)reader.Value;
                        break;
                    case "allowDbNull":
                        _dataColumn.AllowDBNull = (bool)reader.Value;
                        break;
                    case "columnMapping":
                        _dataColumn.ColumnMapping = _serializer.Deserialize<MappingType>(reader);
                        break;
                    case "unique":
                        _dataColumn.Unique = (bool)reader.Value;
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
    }
}
