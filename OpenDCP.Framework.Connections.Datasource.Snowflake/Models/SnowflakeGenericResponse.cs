using OpenDCP.Framework.Configuration.Model.Responses.Datasource;
using System.Collections.Generic;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake.Models
{
    public class SnowflakeGenericResponse<T> : DatasourceResponse
    {
        public List<T> Response { get; set; } = new List<T>();
    }
}
