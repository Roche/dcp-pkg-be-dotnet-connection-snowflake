using OpenDCP.Framework.Connections.Datasource.Snowflake.Constants;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.QueryBuilder
{
    public class SnowflakeQueryBuilder
    {
        private string? _table;
        private string? _database;
        private string? _schema;
        private List<string> _columns = new List<string>();
        private List<Join> _joins = new List<Join>();
        private ICondition? _whereCondition;
        private List<string> _orderBy = new List<string>();
        private List<string> _groupBy = new List<string>();
        private ICondition? _havingCondition;
        private int? _limit;
        private bool _distinct = false;

        public SnowflakeQueryBuilder Select(params string[] columns)
        {
            _columns.AddRange(columns);
            return this;
        }

        public SnowflakeQueryBuilder Database(string database)
        {
            _database = database;
            return this;
        }

        public SnowflakeQueryBuilder Schema(string schema)
        {
            _schema = schema;
            return this;
        }

        public SnowflakeQueryBuilder From(string table)
        {
            _table = table;
            return this;
        }

        public SnowflakeQueryBuilder Distinct()
        {
            _distinct = true;
            return this;
        }

        public SnowflakeQueryBuilder Where(ICondition condition)
        {
            _whereCondition = condition;
            return this;
        }

        public SnowflakeQueryBuilder OrderBy(string column, OrderOperator direction = OrderOperator.ASC)
        {
            _orderBy.Add($"{column} {direction}");
            return this;
        }

        public SnowflakeQueryBuilder GroupBy(List<string> columns)
        {
            _groupBy = columns;
            return this;
        }

        public SnowflakeQueryBuilder Having(ICondition condition)
        {
            _havingCondition = condition;
            return this;
        }

        public SnowflakeQueryBuilder Limit(int limit)
        {
            _limit = limit;
            return this;
        }

        public SnowflakeQueryBuilder Join(string table, string condition, JoinType joinType = JoinType.INNER)
        {
            _joins.Add(new Join(joinType, table, condition));
            return this;
        }

        public SnowflakeQueryBuilder LeftJoin(string table, string condition)
            => Join(table, condition, JoinType.LEFT);

        public SnowflakeQueryBuilder RightJoin(string table, string condition)
            => Join(table, condition, JoinType.RIGHT);

        public SnowflakeQueryBuilder FullJoin(string table, string condition)
            => Join(table, condition, JoinType.FULL);

        public SnowflakeQueryBuilder InnerJoin(string table, string condition)
            => Join(table, condition, JoinType.INNER);

        public string ToSql()
        {
            if (string.IsNullOrEmpty(_table))
                throw new InvalidOperationException("Table name must be specified.");

            var query = new StringBuilder();
            query.Append("SELECT ");

            if (_distinct)
                query.Append("DISTINCT ");

            query.Append(_columns.Count > 0 ? string.Join(", ", _columns) : "*");
            query.Append($" FROM {_schema}.{_database}.{_table}");

            foreach (var join in _joins)
            {
                query.Append($" {join.ToSql()}");
            }

            if (_whereCondition != null)
                query.Append($" WHERE {_whereCondition.ToSql()}");

            if (_groupBy.Count > 0)
                query.Append($" GROUP BY {string.Join(", ", _groupBy)}");

            if (_havingCondition != null)
                query.Append($" HAVING {_havingCondition.ToSql()}");

            if (_orderBy.Count > 0)
                query.Append($" ORDER BY {string.Join(", ", _orderBy)}");

            if (_limit.HasValue)
                query.Append($" LIMIT {_limit.Value}");

            return query.ToString();
        }

        public SnowflakeQueryBuilder Reset()
        {
            _table = null;
            _distinct = false;
            _columns = new List<string>();
            _joins = new List<Join>();
            _whereCondition = null;
            _orderBy = new List<string>();
            _groupBy = new List<string>();
            _havingCondition = null;
            _limit = null;

            return this;
        }
    }
}
