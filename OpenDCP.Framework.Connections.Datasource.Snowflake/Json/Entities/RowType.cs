namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Entities
{
    public class RowType
    {
        public required string Name { get; set; }
        public required string Database { get; set; }
        public required string Schema { get; set; }
        public required string Table { get; set; }
        public long? Scale { get; set; }
        public bool Nullable { get; set; }
        public long? ByteLength { get; set; }
        public long? Length { get; set; }
        public required string Type { get; set; }
        public long? Precision { get; set; }
        public string? Collation { get; set; }
    }
}
