namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Constants
{
    internal enum SnowflakeDataTypes : byte
    {
        Text = 1,
        Fixed = 2,
        Real = 3,
        Boolean = 4,
        Date = 5,
        Time = 6,
        Timestamp_ntz = 7,
        Timestamp_ltz = 8,
        Timestamp_tz = 9,
        Object = 10,
        Variant = 11,
        Array = 12,
        Binary = 13,
    }
}
