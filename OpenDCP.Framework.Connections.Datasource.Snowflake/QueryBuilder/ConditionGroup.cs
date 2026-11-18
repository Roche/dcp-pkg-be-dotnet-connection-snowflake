using OpenDCP.Framework.Connections.Datasource.Snowflake.Constants;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Interfaces;
using System.Collections.Generic;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.QueryBuilder
{
    public class ConditionGroup : ICondition
    {
        private readonly List<ICondition> _conditions = new List<ICondition>();
        private readonly LogicalOperator _logicalOperator;

        public ConditionGroup(LogicalOperator logicalOperator)
        {
            _logicalOperator = logicalOperator;
        }

        public ConditionGroup AddCondition(ICondition condition)
        {
            _conditions.Add(condition);
            return this;
        }

        public string ToSql()
        {
            if (_conditions.Count == 0)
                return "";

            return "(" + string.Join($" {_logicalOperator} ", _conditions.ConvertAll(c => c.ToSql())) + ")";
        }
    }
}
