using OpenDCP.Framework.Connections.Datasource.Snowflake.Constants;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Interfaces;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.QueryBuilder
{
    public class Condition : ICondition
    {
        private readonly string _column;
        private readonly ConditionOperator _operator;
        private readonly dynamic _value;

        public Condition(string column, ConditionOperator @operator, dynamic value)
        {
            _column = column;
            _operator = @operator;
            _value = value;
        }

        private string AddQuotes(dynamic value)
        {
            return _value is string ? $"'{_value}'" : _value.ToString();
        }

        private static string FormatValue(object? value)
        {
            if (value is null)
                return "NULL";

            return value switch
            {
                string s => $"'{s.Replace("'", "''")}'",
                bool b => b ? "TRUE" : "FALSE",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss zzz}'",
                Guid g => $"'{g}'",
                Enum e => Convert.ToInt32(e).ToString(CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => $"'{value.ToString()?.Replace("'", "''")}'"
            };
        }

        public string ToSql()
        {
            string formattedValue = string.Empty;

            if (_operator == ConditionOperator.IN || _operator == ConditionOperator.NOTIN)
            {
                if (!(_value is IEnumerable list) || _value is string)
                    throw new Exception();

                var items = ((IEnumerable)_value).Cast<object>();

                formattedValue = $"({string.Join(',', items.Select(x => FormatValue(x)))})";
            }
            else if (_operator == ConditionOperator.ISNULL || _operator == ConditionOperator.ISNOTNULL)
            {
                formattedValue = string.Empty;
            }
            else
            {
                formattedValue = AddQuotes(_value);
            }

            return $"{_column} {_operator.ToSql()} {formattedValue}";
        }
    }
}
