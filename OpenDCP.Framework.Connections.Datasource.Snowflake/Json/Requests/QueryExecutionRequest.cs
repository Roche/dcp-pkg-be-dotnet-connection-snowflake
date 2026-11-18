using OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Entities;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Requests
{
    internal class QueryExecutionRequest : SnowflakeRequest
    {
        [JsonPropertyName("statement")]
        public required string Statement { get; set; }
        [JsonPropertyName("database")]
        public string? Database { get; set; }
        [JsonPropertyName("schema")]
        public string? Schema { get; set; }
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        [JsonPropertyName("bindings")]
        public Dictionary<string, ParameterBinding> Bindings { get; set; } = new Dictionary<string, ParameterBinding>();
    }
}
