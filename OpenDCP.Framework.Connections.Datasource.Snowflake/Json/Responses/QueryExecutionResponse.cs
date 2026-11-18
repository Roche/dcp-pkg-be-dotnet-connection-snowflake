using OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Entities;
using System.Collections.Generic;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Responses
{
    internal class QueryExecutionResponse : SnowflakeResponse
    {
        public ResultSetMetaData? ResultSetMetaData { get; set; }
        public List<List<string?>> Data { get; set; } = new List<List<string?>>();
        public required string StatementStatusUrl { get; set; }
        public required string RequestId { get; set; }
        public required string SqlState { get; set; }
        public required string StatementHandle { get; set; }
    }
}
