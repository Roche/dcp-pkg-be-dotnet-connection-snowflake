using OpenDCP.Framework.Connections.Datasource.Snowflake.Converters;
using System.Text.Json.Serialization;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Responses
{
    public abstract class SnowflakeResponse
    {
        public required string Message { get; set; }
        public bool Success { get; set; }
        public string? Headers { get; set; }
        [JsonConverter(typeof(QuotedNumbersToIntConverter))]
        public int? Code { get; set; }
    }
}
