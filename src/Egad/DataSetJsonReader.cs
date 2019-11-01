using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Egad
{
    readonly ref struct DataSetJsonReader
    {
        readonly JsonSerializerOptions _options;
        readonly Utf8JsonReader _reader;
        public DataSetJsonReader(JsonSerializerOptions options, ref Utf8JsonReader reader)
        {
             _options = options;
             _reader = reader;
        }

        interface IDataSetLexer 
        {
            void Lex(ref Utf8JsonReader reader);
        }

        abstract class LexerBase : IDataSetLexer
        {
            readonly JsonTokenType _start;
            readonly JsonTokenType _end;

            protected LexerBase(JsonTokenType start, JsonTokenType end)
            {
                _start = start;
                _end = end;
            }

            public void Lex(ref Utf8JsonReader reader)
            {
                if (reader.TokenType != _start) return;
                int depth = 1;
                bool reading;
                do
                {
                    reading = TryHandleToken(ref reader, ref depth, out var lexer);
                    if (lexer != null)
                    {
                        lexer.Lex(ref reader);
                        if (reader.TokenType == _end) depth--;
                    }
                } while (reading && depth > 0);
            }

            protected abstract IDataSetLexer HandleToken(ref Utf8JsonReader reader);

            protected virtual void CompleteElement() { }

            bool TryHandleToken(ref Utf8JsonReader reader, ref int depth, out IDataSetLexer lexer)
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

                    lexer = HandleToken(ref reader);
                    return true;
                }
                return false;
            }
        }

        abstract class ArrayLexerBase : LexerBase
        {
            protected ArrayLexerBase() : base(JsonTokenType.StartArray, JsonTokenType.EndArray)
            {
            }

            protected abstract IDataSetLexer HandleObject(ref Utf8JsonReader reader);

            protected override IDataSetLexer HandleToken(ref Utf8JsonReader reader) => HandleObject(ref reader);
        }

        abstract class ObjectLexerBase : LexerBase
        {
            protected ObjectLexerBase() : base(JsonTokenType.StartObject, JsonTokenType.EndObject)
            {
            }

            protected abstract IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader);

            string _lastPropertyName;
            protected override IDataSetLexer HandleToken(ref Utf8JsonReader reader)
            {
                if (_lastPropertyName == null)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                        _lastPropertyName = reader.GetString();
                    return null;
                }
                else
                {
                    var lexer = HandleProperty(_lastPropertyName, ref reader);
                    _lastPropertyName = null;
                    return lexer;
                }
            }
        }

        class DataSetLexer : ObjectLexerBase
        {
            readonly JsonSerializerOptions _options;
            public DataSet DataSet { get; }
            public DataSetLexer(JsonSerializerOptions options) 
            {
                _options = options;
                DataSet = new DataSet();
            }

            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader)
            {
                switch (propertyName)
                {
                    case "dataSetName":
                        DataSet.DataSetName = reader.GetString();
                        break;
                    case "enforceConstraints":
                        DataSet.EnforceConstraints = reader.GetBoolean();
                        break;
                    case "locale":
                        var locale = CultureInfo.GetCultureInfo(
                            reader.GetString()
                        );
                        if (!CultureInfo.CurrentCulture.Equals(locale))
                        {
                            DataSet.Locale = locale;
                        }
                        break;
                    case "prefix":
                        DataSet.Prefix = reader.GetString();
                        break;
                    case "caseSensitive":
                        if (reader.GetBoolean())
                            DataSet.CaseSensitive = true;
                        break;
                    case "remotingFormat":
                        DataSet.RemotingFormat = JsonSerializer.Deserialize<SerializationFormat>(ref reader, _options);
                        break;
                    case "schemaSerializationMode":
                        DataSet.SchemaSerializationMode = JsonSerializer.Deserialize<SchemaSerializationMode>(ref reader, _options);
                        break;
                    case "namespace":
                        var @namespace = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(@namespace))
                        {
                            DataSet.Namespace = @namespace;
                        }
                        break;
                    case "extendedProperties":
                        return new PropertyCollectionLexer(_options, DataSet.ExtendedProperties);
                    case "tables":
                        return new DataTableCollectionLexer(_options, DataSet.Tables);
                    case "relations":
                        return new DataRelationCollectionLexer(_options, DataSet);
                }

                return null;
            }
        }

        class DataRelationCollectionLexer : ObjectLexerBase
        {
            readonly JsonSerializerOptions _options;
            readonly DataSet _dataSet;
            
            public DataRelationCollectionLexer(JsonSerializerOptions options, DataSet dataSet)
            {
                _options = options;
                _dataSet = dataSet;
            }

            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader) =>
                new DataRelationLexer(_options, _dataSet, propertyName);
        }

        class DataRelationLexer : ObjectLexerBase
        {
            readonly string _name;
            readonly DataSet _dataSet;
            readonly JsonSerializerOptions _options;
            public DataRelationLexer(JsonSerializerOptions options, DataSet dataSet, string name)
            {
                _options = options;
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
            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader)
            {
                switch (propertyName)
                {
                    case "parentColumnNames":
                        _parentColumnNames = JsonSerializer.Deserialize<string[]>(ref reader, _options);
                        AddIfReached();
                        break;
                    case "nested":
                        _nested = reader.GetBoolean();
                        AddIfReached();
                        break;
                    case "childTableName":
                        _childTableName = reader.GetString();
                        AddIfReached();
                        break;
                    case "childColumnNames":
                        _childColumnNames = JsonSerializer.Deserialize<string[]>(ref reader, _options);
                        AddIfReached();
                        break;
                    case "parentTableName":
                        _parentTableName = reader.GetString();
                        AddIfReached();
                        break;
                }
                return null;
            }
        }

        class DataTableCollectionLexer : ObjectLexerBase
        {
            readonly JsonSerializerOptions _options;
            readonly DataTableCollection _dataTables;

            public DataTableCollectionLexer(JsonSerializerOptions options, DataTableCollection dataTables)
            {
                _options = options;
                _dataTables = dataTables;
            }

            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader)
            {
                var dataTable = _dataTables.Add(propertyName);
                return new DataTableLexer(_options, dataTable);
            }
        }

        class DataTableLexer : ObjectLexerBase
        {
            readonly JsonSerializerOptions _options;
            readonly DataTable _dataTable;

            public DataTableLexer(JsonSerializerOptions options, DataTable dataTable)
            {
                _options = options;
                _dataTable = dataTable;
            }

            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader)
            {
                switch (propertyName)
                {
                    case "minimumCapacity":
                        _dataTable.MinimumCapacity = (int)reader.GetInt64();
                        break;
                    case "locale":
                        var locale = CultureInfo.GetCultureInfo(
                            reader.GetString()
                        );
                        if (!CultureInfo.CurrentCulture.Equals(locale))
                        {
                            _dataTable.Locale = locale;
                        }
                        break;
                    case "extendedProperties":
                        return new PropertyCollectionLexer(_options, _dataTable.ExtendedProperties);
                    case "namespace":
                        var @namespace = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(@namespace))
                        {
                            _dataTable.Namespace = @namespace;
                        }
                        break;
                    case "columns":
                        return new DataColumnCollectionLexer(_options, _dataTable.Columns);
                    case "displayExpression":
                        _dataTable.DisplayExpression = reader.GetString();
                        break;
                    case "remotingFormat":
                        _dataTable.RemotingFormat = JsonSerializer.Deserialize<SerializationFormat>(ref reader, _options);
                        break;
                    case "primaryKey":
                        // TODO ensure occurs after columns are set.
                        _dataTable.PrimaryKey = JsonSerializer.Deserialize<string[]>(ref reader, _options)
                            .Select(name => _dataTable.Columns[name])
                            .ToArray();
                        break;
                    case "caseSensitive":
                        if (reader.GetBoolean())
                            _dataTable.CaseSensitive = true;
                        break;
                    case "rows":
                        return new DataRowCollectionLexer(_options, _dataTable);
                    case "prefix":
                        _dataTable.Prefix = reader.GetString();
                        break;
                }
                return null;
            }
        }

        class DataRowCollectionLexer : ArrayLexerBase
        {
            readonly JsonSerializerOptions _options;
            readonly DataTable _dataTable;

            public DataRowCollectionLexer(JsonSerializerOptions options, DataTable dataTable)
            {
                _options = options;
                _dataTable = dataTable;
            }

            protected override IDataSetLexer HandleObject(ref Utf8JsonReader reader)
            {
                var dataRow = _dataTable.NewRow();
                _dataTable.Rows.Add(dataRow);
                return new DataRowLexer(_options, dataRow, _dataTable.Columns.Count);
            }
        }

        class DataRowCellsLexer : ArrayLexerBase
        {
            readonly JsonSerializerOptions _options;
            readonly DataRow _dataRow;
            readonly DataRowState _rowState;
            readonly DataColumnCollection _dataColumns;
            readonly Action<DataRow> _complete;

            public DataRowCellsLexer(JsonSerializerOptions options, DataColumnCollection dataColumns, DataRow dataRow, DataRowState rowState, Action<DataRow> complete)
            {
                _options = options;
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
            protected override IDataSetLexer HandleObject(ref Utf8JsonReader reader)
            {
                var column = _dataColumns[_position];
                if (reader.TokenType == JsonTokenType.Null)
                    _dataRow[column] = DBNull.Value;
                else
                    _dataRow[column] = JsonSerializer.Deserialize(ref reader, column.DataType, _options);
                _position++;
                return null;
            }
        }

        class DataRowLexer : ObjectLexerBase
        {
            readonly JsonSerializerOptions _options;
            readonly DataRow _dataRow;
            readonly int _columnCount;

            public DataRowLexer(JsonSerializerOptions options, DataRow dataRow, int columnCount)
            {
                _options = options;
                _dataRow = dataRow;
                _columnCount = columnCount;
            }

            DataRowState _rowState;
            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader)
            {
                switch (propertyName)
                {
                    case "rowError":
                        _dataRow.RowError = reader.GetString();
                        break;
                    case "rowState":
                        _rowState = JsonSerializer.Deserialize<DataRowState>(ref reader, _options);
                        break;
                    case "currentValues":
                        return new DataRowCellsLexer(
                            _options,
                            _dataRow.Table.Columns,
                            _dataRow,
                            _rowState,
                            _rowState == DataRowState.Unchanged 
                                ? new Action<DataRow>(row => row.AcceptChanges()) 
                                : _ => { }
                        );
                    case "originalValues":
                        return new DataRowCellsLexer(
                            _options,
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
            readonly JsonSerializerOptions _options;
            readonly DataColumnCollection _dataColumns;
            public DataColumnCollectionLexer(JsonSerializerOptions options, DataColumnCollection dataColumns)
            {
                _options = options;
                _dataColumns = dataColumns;
            }

            protected override IDataSetLexer HandleObject(ref Utf8JsonReader reader)
            {
                var dataColumn = new DataColumn();
                _dataColumns.Add(dataColumn);
                return new DataColumnLexer(_options, dataColumn);
            }
        }

        class DataColumnLexer : ObjectLexerBase
        {
            readonly JsonSerializerOptions _options;
            readonly DataColumn _dataColumn;
            public DataColumnLexer(JsonSerializerOptions options, DataColumn dataColumn)
            {
                _options = options;
                _dataColumn = dataColumn;
            }

            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader)
            {
                switch (propertyName)
                {
                    case "readOnly":
                        _dataColumn.ReadOnly = reader.GetBoolean();
                        break;
                    case "prefix":
                        _dataColumn.Prefix = reader.GetString();
                        break;
                    case "namespace":
                        var @namespace = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(@namespace))
                        {
                            _dataColumn.Namespace = @namespace;
                        }
                        break;
                    case "maxLength":
                        _dataColumn.MaxLength = (int)reader.GetInt64();
                        break;
                    case "extendedProperties":
                        return new PropertyCollectionLexer(_options, _dataColumn.ExtendedProperties);
                    case "expression":
                        _dataColumn.Expression = reader.GetString();
                        break;
                    case "dataType":
                        _dataColumn.DataType = TypeCache.GetDataType(reader.GetString());
                        break;
                    case "defaultValue":
                        if (reader.TokenType == JsonTokenType.Null)
                            _dataColumn.DefaultValue = DBNull.Value;

                        else
                        {
                            // TODO ensure called after datatype
                            _dataColumn.DefaultValue = JsonSerializer.Deserialize(ref reader, _dataColumn.DataType, _options);
                        }
                        break;
                    case "dateTimeMode":
                        _dataColumn.DateTimeMode = JsonSerializer.Deserialize<DataSetDateTime>(ref reader, _options);
                        break;
                    case "columnName":
                        _dataColumn.ColumnName = reader.GetString();
                        break;
                    case "autoIncrementStep":
                        _dataColumn.AutoIncrementStep = reader.GetInt64();
                        break;
                    case "caption":
                        _dataColumn.Caption = reader.GetString();
                        break;
                    case "autoIncrementSeed":
                        _dataColumn.AutoIncrementSeed = reader.GetInt64();
                        break;
                    case "autoIncrement":
                        _dataColumn.AutoIncrement = reader.GetBoolean();
                        break;
                    case "allowDbNull":
                        _dataColumn.AllowDBNull = reader.GetBoolean();
                        break;
                    case "columnMapping":
                        _dataColumn.ColumnMapping = JsonSerializer.Deserialize<MappingType>(ref reader, _options);
                        break;
                    case "unique":
                        _dataColumn.Unique = reader.GetBoolean();
                        break;
                }
                return null;
            }
        }

        class PropertyCollectionLexer : ObjectLexerBase
        {
            readonly JsonSerializerOptions _options;
            readonly PropertyCollection _properties;

            public PropertyCollectionLexer(JsonSerializerOptions options, PropertyCollection properties)
            {
                _options = options;
                _properties = properties;
            }

            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader)
            {
                return new PropertyLexer(propertyName, _properties);
            }
        }

        class PropertyLexer : ObjectLexerBase
        {
            readonly string _propertyName;
            readonly PropertyCollection _properties;
            Type _type;

            public PropertyLexer(string propertyName, PropertyCollection properties)
            {
                _propertyName = propertyName;
                _properties = properties;
                _properties[_propertyName] = null;
            }

            protected override IDataSetLexer HandleProperty(string propertyName, ref Utf8JsonReader reader)
            {
                switch (propertyName)
                {
                    case "type":
                        _type = TypeCache.GetDataType(reader.GetString());
                        break;
                    case "value":
                        var value = reader.GetObject(_type);
                        _properties[_propertyName] = value;
                        break;
                }

                return null;
            }
        }

        public DataSet Read()
        {
            var lexer = new DataSetLexer(_options);
            var reader = _reader;
            lexer.Lex(ref reader);
            return lexer.DataSet;
        }
    }
}
