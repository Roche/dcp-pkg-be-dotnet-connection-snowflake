using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Entities
{
    public class ResultSetMetaData
    {
        [JsonPropertyName("numRows")]
        public int NumRows { get; set; }
        [JsonPropertyName("format")]
        public required string Format { get; set; }
        [JsonPropertyName("rowType")]
        public List<RowType> Types { get; set; } = new List<RowType>();
        [JsonPropertyName("partitionInfo")]
        public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>();
    }
}
