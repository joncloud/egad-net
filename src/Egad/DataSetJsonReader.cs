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
            readonly JsonToken _start;
            readonly JsonToken _end;

            protected LexerBase(JsonToken start, JsonToken end)
            {
                _start = start;
                _end = end;
            }

            public void Lex(JsonReader reader)
            {
                if (reader.TokenType != _start) return;
                int depth = 1;
                bool reading = true;
                do
                {
                    reading = TryHandleToken(reader, ref depth, out var lexer);
                    if (lexer != null)
                    {
                        lexer.Lex(reader);
                        if (reader.TokenType == _end) depth--;
                    }
                } while (reading && depth > 0);
            }

            protected abstract IDataSetLexer HandleToken(JsonReader reader);

            protected virtual void CompleteElement() { }

            bool TryHandleToken(JsonReader reader, ref int depth, out IDataSetLexer lexer)
            {
                lexer = null;
                if (reader.Read())
                {
                    if (reader.TokenType == _start) depth++;
                    else if (reader.TokenType == _end)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            CompleteElement();
                            return false;
                        }
                    }

                    lexer = HandleToken(reader);
                    return true;
                }
                return false;
            }
        }

        abstract class ArrayLexerBase : LexerBase
        {
            protected ArrayLexerBase() : base(JsonToken.StartArray, JsonToken.EndArray)
            {
            }

            protected abstract IDataSetLexer HandleObject(JsonReader reader);

            protected override IDataSetLexer HandleToken(JsonReader reader) => HandleObject(reader);
        }

        abstract class ObjectLexerBase : LexerBase
        {
            protected ObjectLexerBase() : base(JsonToken.StartObject, JsonToken.EndObject)
            {
            }

            protected abstract IDataSetLexer HandleProperty(string propertyName, JsonReader reader);

            string _lastPropertyName;
            protected override IDataSetLexer HandleToken(JsonReader reader)
            {
                if (_lastPropertyName == null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                        _lastPropertyName = (string)reader.Value;
                    return null;
                }
                else
                {
                    var lexer = HandleProperty(_lastPropertyName, reader);
                    _lastPropertyName = null;
                    return lexer;
                }
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
                        var @namespace = (string)reader.Value;
                        if (!string.IsNullOrWhiteSpace(@namespace))
                        {
                            DataSet.Namespace = @namespace;
                        }
                        break;
                    case "extendedProperties":
                        return new PropertyCollectionLexer(_serializer, DataSet.ExtendedProperties);
                    case "tables":
                        return new DataTableCollectionLexer(_serializer, DataSet.Tables);
                    case "relations":
                        return new DataRelationCollectionLexer(_serializer, DataSet);
                }

                return null;
            }
        }

        class DataRelationCollectionLexer : ObjectLexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataSet _dataSet;
            
            public DataRelationCollectionLexer(JsonSerializer serializer, DataSet dataSet)
            {
                _serializer = serializer;
                _dataSet = dataSet;
            }

            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader) =>
                new DataRelationLexer(_serializer, _dataSet, propertyName);
        }

        class DataRelationLexer : ObjectLexerBase
        {
            readonly string _name;
            readonly DataSet _dataSet;
            readonly JsonSerializer _serializer;
            public DataRelationLexer(JsonSerializer serializer, DataSet dataSet, string name)
            {
                _serializer = serializer;
                _dataSet = dataSet;
                _name = name;
            }

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
                var parentTable = _dataSet.Tables[_parentTableName];
                var childTable = _dataSet.Tables[_childTableName];
                _dataSet.Relations.Add(
                    new DataRelation(
                        _name,
                        _parentColumnNames.Select(col => parentTable.Columns[col]).ToArray(),
                        _childColumnNames.Select(col => childTable.Columns[col]).ToArray(),
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
                        var @namespace = (string)reader.Value;
                        if (!string.IsNullOrWhiteSpace(@namespace))
                        {
                            _dataTable.Namespace = @namespace;
                        }
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

        class DataRowCellsLexer : ArrayLexerBase
        {
            readonly JsonSerializer _serializer;
            readonly DataRow _dataRow;
            readonly DataRowState _rowState;
            readonly DataColumnCollection _dataColumns;
            readonly Action<DataRow> _complete;

            public DataRowCellsLexer(JsonSerializer serializer, DataColumnCollection dataColumns, DataRow dataRow, DataRowState rowState, Action<DataRow> complete)
            {
                _serializer = serializer;
                _dataRow = dataRow;
                _rowState = rowState;
                _dataColumns = dataColumns;
                _complete = complete;
            }

            protected override void CompleteElement()
            {
                _complete(_dataRow);
            }

            int _position;
            protected override IDataSetLexer HandleObject(JsonReader reader)
            {
                var column = _dataColumns[_position];
                if (reader.TokenType == JsonToken.Null)
                    _dataRow[column] = DBNull.Value;
                else
                    _dataRow[column] = _serializer.Deserialize(reader, column.DataType);
                _position++;
                return null;
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
                        return new DataRowCellsLexer(
                            _serializer,
                            _dataRow.Table.Columns,
                            _dataRow,
                            _rowState,
                            _rowState == DataRowState.Unchanged 
                                ? new Action<DataRow>(row => row.AcceptChanges()) 
                                : _ => { }
                        );
                    case "originalValues":
                        return new DataRowCellsLexer(
                            _serializer,
                            _dataRow.Table.Columns,
                            _dataRow,
                            _rowState,
                            _rowState == DataRowState.Deleted
                                ? new Action<DataRow>(row =>
                                {
                                    row.AcceptChanges();
                                    row.Delete();
                                })
                                : row => row.AcceptChanges()
                        );
                }
                return null;
            }

            void SetRowValues(object[] rowValues)
            {
                int i = _columnCount;
                while (--i > -1)
                {
                    _dataRow[i] = rowValues[i] ?? DBNull.Value;
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
                        var @namespace = (string)reader.Value;
                        if (!string.IsNullOrWhiteSpace(@namespace))
                        {
                            _dataColumn.Namespace = @namespace;
                        }
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
                        _dataColumn.DataType = TypeCache.GetDataType((string)reader.Value);
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

        class PropertyCollectionLexer : ObjectLexerBase
        {
            readonly JsonSerializer _serializer;
            readonly PropertyCollection _properties;

            public PropertyCollectionLexer(JsonSerializer serializer, PropertyCollection properties)
            {
                _serializer = serializer;
                _properties = properties;
            }

            protected override IDataSetLexer HandleProperty(string propertyName, JsonReader reader)
            {
                var value = _serializer.Deserialize(reader);
                _properties.Add(propertyName, value);
                return null;
            }
        }

        public DataSet Read()
        {
            var lexer = new DataSetLexer(_serializer);
            lexer.Lex(_reader);
            return lexer.DataSet;
        }
    }
}
