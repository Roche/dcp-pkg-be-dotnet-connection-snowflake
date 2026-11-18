using System.Collections.Generic;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Entities
{
    internal class PartitionData
    {
        public List<List<string?>> Data { get; set; } = new List<List<string?>>();
    }
}
