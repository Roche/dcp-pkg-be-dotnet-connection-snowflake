using System.Data;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Extensions
{
    public static class IDataRecordExtension
    {
        public static string? GetNullableString(this IDataRecord row, int i)
        {
            if (row.IsDBNull(i))
            {
                return null;
            }
            else
            {
                return row.GetString(i);
            }
        }
    }
}
