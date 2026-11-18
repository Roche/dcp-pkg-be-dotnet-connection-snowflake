using System;
using System.Reflection;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Constants
{
    // Custom attribute to hold SQL keyword
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class SqlAttribute : Attribute
    {
        public string SqlKeyword { get; }
        public SqlAttribute(string sqlKeyword)
        {
            SqlKeyword = sqlKeyword;
        }
    }

    public enum ConditionOperator
    {
        [Sql("=")]
        EQUAL = 1,
        [Sql("<=")]
        LESSOREQUAL = 2,
        [Sql("<")]
        LESS = 3,
        [Sql(">=")]
        GREATEROREQUAL = 4,
        [Sql(">")]
        GREATER = 5,
        [Sql("!=")]
        NEQUAL = 6,
        [Sql("!=")]
        NOT = 7,
        [Sql("LIKE")]
        LIKE = 8,
        [Sql("IN")]
        IN = 9,
        [Sql("IS NULL")]
        ISNULL = 10,
        [Sql("IS NOT NULL")]
        ISNOTNULL = 11,
        [Sql("NOT LIKE")]
        NOTLIKE = 12,
        [Sql("NOT IN")]
        NOTIN = 13,
    }

    public static class ConditionOperatorExtensions
    {
        public static string ToSql(this ConditionOperator op)
        {
            // Get the FieldInfo for the enum value
            var fieldInfo = op.GetType().GetField(op.ToString());

            if (fieldInfo is null)
                throw new ArgumentException("Attribute must not be null.", nameof(fieldInfo));

            // Get the custom attribute applied to the enum field
            var attribute = (SqlAttribute?)fieldInfo.GetCustomAttribute(typeof(SqlAttribute));

            if (attribute is null)
                throw new ArgumentException("Attribute must not be null.", nameof(op));

            // Return the SQL keyword from the attribute
            return attribute!.SqlKeyword;
        }
    }
}
