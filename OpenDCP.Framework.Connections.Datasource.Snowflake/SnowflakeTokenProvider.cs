using Microsoft.Extensions.Caching.Memory;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Models;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.HttpClient;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake
{
    internal sealed class SnowflakeTokenProvider
    {
        private const string TokenCacheKey = "snowflakeTokenDetails";
        private readonly DCPHttpClient _httpClient;
        private readonly IMemoryCache? _memoryCache;


        public SnowflakeTokenProvider(DCPHttpClient httpClient, IMemoryCache? memoryCache = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _memoryCache = memoryCache;
        }


        public async Task<string> GetTokenAsync(JsonObject connectionString)
        {
            if (_memoryCache != null && IsTokenValid())
            {
                return _memoryCache.Get<SnowflakeTokenDetails>(TokenCacheKey)!.OauthToken;
            }


            var token = await FetchOAuthToken(connectionString);
            return token.OauthToken;
        }


        private bool IsTokenValid()
        {
            var token = _memoryCache?.Get<SnowflakeTokenDetails>(TokenCacheKey);
            return token != null && token.ExpiresAt > DateTime.UtcNow;
        }


        private async Task<SnowflakeTokenDetails> FetchOAuthToken(JsonObject cs)
        {
            string role;

            if (!string.IsNullOrEmpty(cs["Role"]?.ToString()))
            {
                role = "role:" + cs["Role"]?.ToString();
            }
            else
            {
                role = "role-any";
            }

            var payload = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "scope", $"session:{role}" },
                { "username", GetMandatoryField(cs, "Username") },
                { "password", GetMandatoryField(cs, "Password") }
            };

            var content = new FormUrlEncodedContent(payload);

            var response = await _httpClient.PostAsync(
                GetMandatoryField(cs, "OAuthUrl"),
                content,
                h =>
                {
                    if (!string.IsNullOrEmpty(_httpClient.OriginHeader))
                        h.Add("Origin", _httpClient.OriginHeader);


                    h.Authorization = CreateBasicAuthHeader(cs);
                });

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OAuth token request failed: {response.StatusCode}");


            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var expiresIn = json.RootElement.GetProperty("expires_in").GetDouble();

            var token = new SnowflakeTokenDetails
            {
                OauthToken = json.RootElement.GetProperty("access_token").GetString()!,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60)
            };

            _memoryCache?.Set(TokenCacheKey, token, token.ExpiresAt);

            return token;
        }

        private static string GetMandatoryField(JsonObject obj, string key) =>
            obj[key]?.ToString() ?? throw new DCPBadRequestException($"Missing field: {key}");


        private static AuthenticationHeaderValue CreateBasicAuthHeader(JsonObject cs)
        {
            var credentials = $"{GetMandatoryField(cs, "ClientId")}:{GetMandatoryField(cs, "ClientSecret")}";

            return new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
        }
    }
}
