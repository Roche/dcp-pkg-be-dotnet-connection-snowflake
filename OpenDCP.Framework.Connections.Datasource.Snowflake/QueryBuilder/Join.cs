using OpenDCP.Framework.Connections.Datasource.Snowflake.Constants;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.QueryBuilder
{
    public class Join
    {
        public JoinType JoinType { get; }
        public string Table { get; }
        public string Condition { get; }

        public Join(JoinType joinType, string table, string condition)
        {
            JoinType = joinType;
            Table = table;
            Condition = condition;
        }

        public string ToSql() => $"{JoinType} JOIN {Table} ON {Condition}";
    }
}
