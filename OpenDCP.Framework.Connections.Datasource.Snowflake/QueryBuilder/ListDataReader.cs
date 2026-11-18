using OpenDCP.Framework.Connections.Datasource.Snowflake.Constants;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.QueryBuilder
{
    public class ListDataReader : IDataReader
    {
        private readonly List<List<string?>> _data;
        private readonly List<RowType> _columns;
        private int _currentIndex = -1;

        public ListDataReader(List<List<string?>> data, List<RowType> columns)
        {
            _data = data;
            _columns = columns;
        }

        public bool Read() => ++_currentIndex < _data.Count;

        public bool IsDBNull(int i)
        {
            var raw = _data[_currentIndex][i];
            if (raw is null)
            {
                return true;

            }
            else
            {
                return false;
            }
        }
        public object GetValue(int i)
        {
            var raw = _data[_currentIndex][i];
            if (raw is null) return DBNull.Value;

            var targetType = MapSnowflakeType(_columns[i].Type);
            try
            {
                if (targetType == typeof(string))
                    return raw;
                if (targetType == typeof(double))
                    return double.Parse(raw, CultureInfo.InvariantCulture);
                if (targetType == typeof(decimal))
                    return decimal.Parse(raw, CultureInfo.InvariantCulture);
                if (targetType == typeof(int))
                    return int.Parse(raw, CultureInfo.InvariantCulture);
                if (targetType == typeof(bool))
                    return raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw == "1";
                if (targetType == typeof(DateTimeOffset))
                {
                    var unixTime = double.Parse(raw, CultureInfo.InvariantCulture);
                    long seconds = (long)Math.Floor(unixTime);
                    double fractionalSeconds = unixTime - seconds;

                    // Unix epoch start
                    DateTime epoch = DateTime.UnixEpoch;

                    // Add seconds and fractional seconds
                    return DateTime.SpecifyKind(epoch.AddSeconds(seconds).AddSeconds(fractionalSeconds), DateTimeKind.Utc);
                }
                if (targetType == typeof(TimeSpan))
                    return TimeSpan.Parse(raw, CultureInfo.InvariantCulture);
                if (targetType == typeof(byte[]))
                    return Convert.FromBase64String(raw); // If binary is Base64 encoded
            }
            catch
            {
                return DBNull.Value;
            }

            return raw;
        }

        public int FieldCount => _columns.Count;
        public string GetName(int i) => _columns[i].Name;
        public Type GetFieldType(int i) => MapSnowflakeType(_columns[i].Type);
        public int GetOrdinal(string name) => Array.IndexOf(_columns.Select(c => c.Name).ToArray(), name);
        public object this[int i] => GetValue(i);
        public object this[string name] => GetValue(GetOrdinal(name));

        // --- Not used or return fixed values ---
        public void Close() { }
        public int Depth => 0;
        public DataTable GetSchemaTable() => throw new NotImplementedException();
        public bool IsClosed => false;
        public bool NextResult() => false;
        public int RecordsAffected => -1;

        public void Dispose() { }

        // --- Strong typed accessors (simplified) ---
        public string GetString(int i) => (string)GetValue(i);
        public double GetDouble(int i) => (double)GetValue(i);
        public short GetInt16(int i) => (short)GetValue(i);
        public int GetInt32(int i) => (int)GetValue(i);
        public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
        public long GetInt64(int i) => (long)GetValue(i);
        public float GetFloat(int i) => (float)GetValue(i);
        public decimal GetDecimal(int i) => (decimal)GetValue(i);
        public bool GetBoolean(int i) => (bool)GetValue(i);
        public byte GetByte(int i) => (byte)GetValue(i);
        public char GetChar(int i) => (char)GetValue(i);
        public Guid GetGuid(int i) => (Guid)GetValue(i);

        // Unused
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();
        public Type GetDataTypeName(int i) => MapSnowflakeType(_columns[i].Type);

        string IDataRecord.GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        private static Type MapSnowflakeType(SnowflakeDataTypes type)
        {
            return type switch
            {
                SnowflakeDataTypes.Text => typeof(string),
                SnowflakeDataTypes.Fixed => typeof(decimal),
                SnowflakeDataTypes.Real => typeof(double),
                SnowflakeDataTypes.Boolean => typeof(bool),
                SnowflakeDataTypes.Date => typeof(DateTime),
                SnowflakeDataTypes.Time => typeof(TimeSpan),
                SnowflakeDataTypes.Timestamp_ntz => typeof(DateTime),
                SnowflakeDataTypes.Timestamp_ltz => typeof(DateTimeOffset),
                SnowflakeDataTypes.Timestamp_tz => typeof(DateTimeOffset),
                SnowflakeDataTypes.Object => typeof(string),
                SnowflakeDataTypes.Variant => typeof(string),
                SnowflakeDataTypes.Array => typeof(string),
                SnowflakeDataTypes.Binary => typeof(byte[]),
                _ => typeof(string)
            };
        }

        private static Type MapSnowflakeType(string type)
        {
            return type switch
            {
                "text" => typeof(string),
                "fixed" => typeof(decimal),
                "real" => typeof(double),
                "boolean" => typeof(bool),
                "date" => typeof(DateTime),
                "time" => typeof(TimeSpan),
                "timestamp_ntz" => typeof(DateTimeOffset),
                "timestamp_ltz" => typeof(DateTimeOffset),
                "timestamp_tz" => typeof(DateTimeOffset),
                "object" => typeof(string),
                "variant" => typeof(string),
                "array" => typeof(string),
                "binary" => typeof(byte[]),
                _ => typeof(string)
            };
        }
    }

}
