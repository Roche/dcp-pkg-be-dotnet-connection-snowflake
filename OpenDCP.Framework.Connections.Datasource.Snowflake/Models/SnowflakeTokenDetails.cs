using System;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Models
{
    public class SnowflakeTokenDetails
    {
        public required string OauthToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
