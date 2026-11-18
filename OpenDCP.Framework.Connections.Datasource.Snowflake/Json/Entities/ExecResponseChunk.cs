namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Entities
{
    public class PartitionInfo
    {
        public string? Url { get; set; }
        public int RowCount { get; set; }
        public long UncompressedSize { get; set; }
    }
}
