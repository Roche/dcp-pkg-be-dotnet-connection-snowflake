using Microsoft.Extensions.Caching.Memory;
using OpenDCP.Framework.Configuration.Model.Errors;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Entities;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Requests;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Json.Responses;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Models;
using OpenDCP.Framework.Connections.Datasource.Snowflake.QueryBuilder;
using OpenDCP.Framework.HttpClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake
{
    internal sealed class SnowflakeQueryExecutor
    {
        private readonly DCPHttpClient _httpClient;
        private readonly SnowflakeTokenProvider _tokenProvider;

        public SnowflakeQueryExecutor(DCPHttpClient httpClient, IMemoryCache? memoryCache)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenProvider = new SnowflakeTokenProvider(_httpClient, memoryCache);
        }

        public async Task<SnowflakeGenericResponse<T>> ExecuteAsync<T>(
            JsonObject connectionString,
            string query,
            Dictionary<string, object>? parameters = null,
            Dictionary<string, string>? mappings = null,
            Func<IDataReader, T>? mappingFunc = null) where T : new()
        {
            var result = new SnowflakeGenericResponse<T>();
            QueryExecutionResponse response;

            try
            {
                response = await QueryInternalAsync(connectionString, query, parameters);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new DatasourceError
                {
                    Error = ex.Message,
                });
                return result;
            }

            if (!response.Success)
            {
                result.Errors.Add(new DatasourceError
                {
                    Error = response.Message,
                });
                return result;
            }


            try
            {
                using var reader = new ListDataReader(response.Data, response.ResultSetMetaData!.Types);

                if (mappingFunc != null)
                {
                    while (reader.Read())
                    {
                        result.Response.Add(mappingFunc(reader));
                    }
                }
                else
                {
                    MapByReflection(reader, result.Response, mappings);
                }
            }

            catch (Exception ex)
            {
                result.Errors.Add(new DatasourceError { Error = ex.Message });
            }


            return result;
        }

        private async Task<QueryExecutionResponse> QueryInternalAsync(
            JsonObject connectionString,
            string sql,
            Dictionary<string, object>? parameters = null)
        {
            var token = await _tokenProvider.GetTokenAsync(connectionString);

            var request = new QueryExecutionRequest { Statement = sql };

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    request.Bindings[parameter.Key] = new ParameterBinding()
                    {
                        Type = GetSnowflakeBindingType(parameter.Value),
                        Value = parameter.Value?.ToString() ?? string.Empty
                    };
                }
            }

            if (!string.IsNullOrEmpty(connectionString["Role"]?.ToString()))
            {
                request.Role = connectionString["Role"]?.ToString();
            }

            if (!string.IsNullOrEmpty(connectionString["Schema"]?.ToString()))
            {
                request.Schema = connectionString["Schema"]?.ToString();
            }

            if (!string.IsNullOrEmpty(connectionString["Database"]?.ToString()))
            {
                request.Database = connectionString["Database"]?.ToString();
            }

            var response = await _httpClient.PostAsync(
                uri: $"{GetBaseUrl(connectionString).TrimEnd('/')}/api/v2/statements",
                data: JsonSerializer.Serialize(request),
                headers =>
                {
                    if (!string.IsNullOrEmpty(_httpClient.OriginHeader))
                        headers.Add("Origin", _httpClient.OriginHeader);

                    headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    AddHeaders(headers, connectionString);
                });

            var payload = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Snowflake API returned {(int)response.StatusCode} {response.ReasonPhrase}. Raw Error Payload: {payload}", null, response.StatusCode);
            }

            var result = JsonSerializer.Deserialize<QueryExecutionResponse>(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result is null)
            {
                throw new JsonException($"Snowflake payload couldn't be mapped. Raw payload: {payload}");
            }

            await LoadPartitionsAsync(result, token, connectionString);

            result.Success = true;
            return result;
        }

        private async Task LoadPartitionsAsync(QueryExecutionResponse result, string token, JsonObject connectionString)
        {
            var partitions = result.ResultSetMetaData!.Partitions;
            if (partitions.Count <= 1) return;

            var tasks = Enumerable.Range(1, partitions.Count - 1)
                .Select(i => GetPartitionDataAsync(result.StatementHandle, i, token, connectionString));

            foreach (var (data, success) in await Task.WhenAll(tasks))
            {
                if (!success)
                {
                    result.Success = false;
                    return;
                }

                result.Data.AddRange(data!.Data);
            }
        }

        private async Task<(PartitionData? data, bool success)> GetPartitionDataAsync(string handle, int partition, string token, JsonObject connectionString)
        {
            var response = await _httpClient.GetAsync(
                uri: $"{GetBaseUrl(connectionString).TrimEnd('/')}/api/v2/statements/{handle}?partition={partition}",
                headers =>
                {
                    if (!string.IsNullOrEmpty(_httpClient.OriginHeader))
                        headers.Add("Origin", _httpClient.OriginHeader);

                    headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    AddHeaders(headers, connectionString);
                });

            if (!response.IsSuccessStatusCode) return (null, false);

            string json;
            var contentEncoding = response.Content.Headers.ContentEncoding.ToString();

            // Check if server sent a gzipped payload
            if (contentEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase))
            {
                using var compressedStream = await response.Content.ReadAsStreamAsync();
                using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                using var reader = new StreamReader(decompressionStream);
                json = await reader.ReadToEndAsync();
            }
            else
            {
                // Plain JSON response
                json = await response.Content.ReadAsStringAsync();
            }

            if (string.IsNullOrWhiteSpace(json)) return (null, false);

            var data = JsonSerializer.Deserialize<PartitionData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return (data, data != null);
        }

        private static void MapByReflection<T>(
            IDataReader reader,
            IList<T> target,
            Dictionary<string, string>? mappings) where T : new()
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(i => reader.GetName(i))
            .ToDictionary(n => n, StringComparer.OrdinalIgnoreCase);


            while (reader.Read())
            {
                var instance = new T();
                foreach (var prop in properties)
                {
                    var columnName = mappings != null && mappings.TryGetValue(prop.Name, out var mapped)
                    ? mapped
                    : prop.Name;


                    if (!columns.TryGetValue(columnName, out var actual)) continue;


                    var value = reader[actual];
                    if (value == DBNull.Value) continue;


                    var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    prop.SetValue(instance, Convert.ChangeType(value, type));
                }
                target.Add(instance);
            }
        }

        private static void AddHeaders(HttpRequestHeaders headers, JsonObject connectionString)
        {
            headers.Add("Accept", "*/*");
            headers.Add("User-Agent", "DCP");

            var proxy = connectionString["Proxy"] as JsonObject;

            var isProxyActive =
                proxy == null
                    ? false
                    : proxy["Active"] == null ||
                      string.Equals(
                          proxy["Active"]!.ToString(),
                          "true",
                          StringComparison.OrdinalIgnoreCase
                      );

            if (isProxyActive)
            {
                headers.Add("client_id", connectionString["Proxy"]!["ClientId"]!.ToString());
                headers.Add("client_secret", connectionString["Proxy"]!["ClientSecret"]!.ToString());
            }
        }

        private static string GetBaseUrl(JsonObject connectionString)
        {
            var proxy = connectionString["Proxy"] as JsonObject;

            var isProxyActive =
                proxy == null
                    ? false
                    : proxy["Active"] == null ||
                      string.Equals(
                          proxy["Active"]!.ToString(),
                          "true",
                          StringComparison.OrdinalIgnoreCase
                      );

            return isProxyActive
                ? proxy?["Url"]?.ToString() ?? throw new Exception("Proxy Url is required when Proxy is active.")
                : $"https://{GetRequired(connectionString, "Account")}.{GetRequired(connectionString, "Region")}.snowflakecomputing.com/";
        }

        private static string GetRequired(JsonObject obj, string key) =>
            obj[key]?.ToString() ?? throw new InvalidOperationException($"Missing required field: {key}");

        private static string GetSnowflakeBindingType(object? value)
        {
            return value switch
            {
                bool => "BOOLEAN",
                byte or sbyte or short or ushort or int or long or ulong => "FIXED",
                float or double => "REAL",
                decimal => "DECFLOAT",
                _ => "TEXT"
            };
        }
    }
}
