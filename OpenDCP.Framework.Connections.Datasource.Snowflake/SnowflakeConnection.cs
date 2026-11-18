using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using OpenDCP.Framework.Configuration.Model.Interfaces;
using OpenDCP.Framework.Configuration.Model.Requests;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Constants;
using OpenDCP.Framework.Connections.Datasource.Snowflake.Models;
using OpenDCP.Framework.Datasource.Base;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.FormBuilder.Constants;
using OpenDCP.Framework.FormBuilder.Models;
using OpenDCP.Framework.HttpClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Framework.Connections.Datasource.Snowflake
{
    public abstract class SnowflakeConnection : BaseDatasource, IRelationDataSource
    {
        private readonly SnowflakeQueryExecutor _queryExecutor;

        private const string ConnectionStringProxyKey = "Proxy";

        protected SnowflakeConnection(IConfiguration configuration) : base(configuration)
        {
            var httpClient = new DCPHttpClient(_configuration);
            _queryExecutor = new SnowflakeQueryExecutor(httpClient, null);
        }
        protected SnowflakeConnection(IConfiguration configuration, IMemoryCache memoryCache) : base(configuration)
        {
            var httpClient = new DCPHttpClient(_configuration);
            _queryExecutor = new SnowflakeQueryExecutor(httpClient, memoryCache);
        }
        protected SnowflakeConnection(int? id, JsonObject connectionString, IConfiguration configuration) : base(id, connectionString, configuration)
        {
            var httpClient = new DCPHttpClient(_configuration);
            _queryExecutor = new SnowflakeQueryExecutor(httpClient, null);
        }
        protected SnowflakeConnection(int? id, JsonObject connectionString, IConfiguration configuration, IMemoryCache memoryCache) : base(id, connectionString, configuration)
        {
            var httpClient = new DCPHttpClient(_configuration);
            _queryExecutor = new SnowflakeQueryExecutor(httpClient, memoryCache);
        }

        protected override List<InputField> ConnectionStringFields
        {
            get
            {
                var connectionFields = new List<InputField>();
                connectionFields.AddRange(SchemaConnectionStringFields);
                connectionFields.AddRange(new List<InputField>
                {
                    new InputField
                    {
                        Name = "Username",
                        Label = "Username",
                        FieldType = (byte)InputFieldType.String,
                        FieldValidations = new List<FieldValidationDTO>
                        {
                            new FieldValidationDTO
                            {
                                Type = InputFieldValidationType.Required.ToString(),
                            }
                        }
                    },
                    new InputField
                    {
                        Name = "Password",
                        Label = "Password",
                        FieldType = (byte)InputFieldType.Secret,
                        FieldValidations = new List<FieldValidationDTO>
                        {
                            new FieldValidationDTO
                            {
                                Type = InputFieldValidationType.Required.ToString(),
                            }
                        }
                    },
                    new InputField
                    {
                        Name = "OAuthUrl",
                        Label = "OAuth URL endpoint",
                        FieldType = (byte)InputFieldType.String,
                        FieldValidations = new List<FieldValidationDTO>
                        {
                            new FieldValidationDTO
                            {
                                Type = InputFieldValidationType.Required.ToString(),
                            }
                        }
                    },
                    new InputField
                    {
                        Name = "ClientId",
                        Label = "ClientId for OAuth token request",
                        FieldType = (byte)InputFieldType.String,
                        FieldValidations = new List<FieldValidationDTO>
                        {
                            new FieldValidationDTO
                            {
                                Type = InputFieldValidationType.Required.ToString(),
                            }
                        }
                    },
                    new InputField
                    {
                        Name = "ClientSecret",
                        Label = "Client secret for OAuth token request",
                        FieldType = (byte)InputFieldType.Secret,
                        FieldValidations = new List<FieldValidationDTO>
                        {
                            new FieldValidationDTO
                            {
                                Type = InputFieldValidationType.Required.ToString(),
                            }
                        }
                    },
                    new InputField
                    {
                        Name = "Region",
                        Label = "Region",
                        FieldType = (byte)InputFieldType.String,
                        FieldValidations = new List<FieldValidationDTO>
                        {
                            new FieldValidationDTO
                            {
                                Type = InputFieldValidationType.Required.ToString(),
                            }
                        },
                        ConditionGroups = new List<FieldConditionGroup>
                        {
                            new FieldConditionGroup
                            {
                                Operator = FormBuilder.Constants.LogicalOperator.And,
                                Action = FieldConditionAction.Hide,
                                LoadUrl = null,
                                LoadOnce = true,
                                Conditions = new List<FieldCondition>
                                {
                                    new FieldCondition
                                    {
                                        Operator = FieldConditionOperator.Equal,
                                        DependentOn = "Proxy.Active",
                                        Values = new List<dynamic> { true }
                                    }
                                }
                            }
                        }
                    },
                    new InputField
                    {
                        Name = "Account",
                        Label = "Account",
                        FieldType = (byte)InputFieldType.String,
                        FieldValidations = new List<FieldValidationDTO>
                        {
                            new FieldValidationDTO
                            {
                                Type = InputFieldValidationType.Required.ToString(),
                            }
                        },
                        ConditionGroups = new List<FieldConditionGroup>
                        {
                            new FieldConditionGroup
                            {
                                Operator = FormBuilder.Constants.LogicalOperator.And,
                                Action = FieldConditionAction.Hide,
                                LoadUrl = null,
                                LoadOnce = true,
                                Conditions = new List<FieldCondition>
                                {
                                    new FieldCondition
                                    {
                                        Operator = FieldConditionOperator.Equal,
                                        DependentOn = "Proxy.Active",
                                        Values = new List<dynamic> { true }
                                    }
                                }
                            }
                        }
                    },
                    new InputField
                    {
                        Name = "Role",
                        Label = "Role",
                        FieldType = (byte)InputFieldType.String,
                    },
                    new InputField
                    {
                        Name = "Proxy",
                        Label = "Proxy",
                        FieldType = (byte)InputFieldType.Group,
                        Children = new List<InputField>
                        {
                            new InputField
                            {
                                Name = "Active",
                                Label = "Proxy Active",
                                FieldType = (byte)InputFieldType.Checkbox,
                                FieldSettings = new FieldSettings
                                {
                                    DefaultValue = false
                                },
                            },
                            new InputField
                            {
                                Name = "Url",
                                Label = "Proxy Url",
                                FieldType = (byte)InputFieldType.String,
                                ConditionGroups = new List<FieldConditionGroup>
                                {
                                    new FieldConditionGroup
                                    {
                                        Operator = FormBuilder.Constants.LogicalOperator.And,
                                        Action = FieldConditionAction.Show,
                                        LoadUrl = null,
                                        LoadOnce = true,
                                        Conditions = new List<FieldCondition>
                                        {
                                            new FieldCondition
                                            {
                                                Operator = FieldConditionOperator.Equal,
                                                DependentOn = "Active",
                                                Values = new List<dynamic> { true }
                                            }
                                        }
                                    }
                                },
                                FieldValidations = new List<FieldValidationDTO>
                                {
                                    new FieldValidationDTO
                                    {
                                        Type = InputFieldValidationType.Required.ToString(),
                                    }
                                },
                            },
                            new InputField
                            {
                                Name = "ClientId",
                                Label = "Proxy Client Id",
                                FieldType = (byte)InputFieldType.String,
                                ConditionGroups = new List<FieldConditionGroup>
                                {
                                    new FieldConditionGroup
                                    {
                                        Operator = FormBuilder.Constants.LogicalOperator.And,
                                        Action = FieldConditionAction.Show,
                                        LoadUrl = null,
                                        LoadOnce = true,
                                        Conditions = new List<FieldCondition>
                                        {
                                            new FieldCondition
                                            {
                                                Operator = FieldConditionOperator.Equal,
                                                DependentOn = "Active",
                                                Values = new List<dynamic> { true }
                                            }
                                        }
                                    }
                                },
                                FieldValidations = new List<FieldValidationDTO>
                                {
                                    new FieldValidationDTO
                                    {
                                        Type = InputFieldValidationType.Required.ToString(),
                                    }
                                },
                            },
                            new InputField
                            {
                                Name = "ClientSecret",
                                Label = "Proxy Client Secret",
                                FieldType = (byte)InputFieldType.Secret,
                                ConditionGroups = new List<FieldConditionGroup>
                                {
                                    new FieldConditionGroup
                                    {
                                        Operator = FormBuilder.Constants.LogicalOperator.And,
                                        Action = FieldConditionAction.Show,
                                        LoadUrl = null,
                                        LoadOnce = true,
                                        Conditions = new List<FieldCondition>
                                        {
                                            new FieldCondition
                                            {
                                                Operator = FieldConditionOperator.Equal,
                                                DependentOn = "Active",
                                                Values = new List<dynamic> { true }
                                            }
                                        }
                                    }
                                },
                                FieldValidations = new List<FieldValidationDTO>
                                {
                                    new FieldValidationDTO
                                    {
                                        Type = InputFieldValidationType.Required.ToString(),
                                    }
                                },
                            }
                        }
                    }
                });
                return connectionFields;
            }
        }
        protected virtual List<InputField> SchemaConnectionStringFields
        {
            get
            {
                return new List<InputField>();
            }
        }
        protected override List<string> SecretFields => new List<string> { "Password", "ClientSecret" };
        protected override List<string> SecretDistributionFields => new List<string> { "password", "clientsecret", "username", "clientid" };
        public override string DisplayConnectionString()
        {
            return $"Database: {GetMandatoryField(_connectionString, "database")} in schema {GetMandatoryField(_connectionString, "schema")}";
        }

        public async Task ValidateConnectionString(JsonObject connectionString)
        {
            const string query = "Select 1 as value";

            var responseQuery = await _queryExecutor.ExecuteAsync<int>(connectionString, query, null, null);

            if (responseQuery.Errors.Count > 0)
            {
                throw new DCPBadRequestException(ErrorMessages.ConnectionToSnowflakeFailed);
            }

            await ValidateDatabaseSchema(connectionString);
        }

        protected virtual Task ValidateDatabaseSchema(JsonObject connectionString)
        {
            return Task.CompletedTask;
        }

        public async Task<SnowflakeGenericResponse<T>> ExecuteQueryAsync<T>(
            string query,
            Dictionary<string, string>? mappings = null,
            Dictionary<string, object>? parameters = null,
            Func<IDataReader, T>? mappingFun = null,
            JsonObject? connectionString = null) where T : new()
        {
            var conenctionString = connectionString ?? _connectionString;

            return await _queryExecutor.ExecuteAsync(
                connectionString: conenctionString!,
                query: query,
                parameters: parameters,
                mappings: mappings,
                mappingFunc: mappingFun);
        }

        protected static string GetMandatoryField(JsonObject? obj, string key)
        {
            if (obj is null)
                throw new ArgumentNullException(nameof(obj));

            return obj[key]?.ToString() ?? throw new DCPBadRequestException($"Missing field: {key}");
        }

        protected string GetMandatoryField(string key)
        {
            return GetMandatoryField(_connectionString, key);
        }

        public async Task<Dictionary<string, List<string>>> FindByQueryAsync(string query, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<string, List<string>>();

            Func<IDataReader, KeyValuePair<string?, string?>> mappingFunction = reader =>
            {
                var key = reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString();
                var value = reader.IsDBNull(1) ? null : reader.GetValue(1)?.ToString();
                return new KeyValuePair<string?, string?>(key, value);
            };

            var genericResponse = await ExecuteQueryAsync(
                query: query,
                mappingFun: mappingFunction
            );

            foreach (var pair in genericResponse.Response)
            {
                if (pair.Key == null) continue;

                if (!result.TryGetValue(pair.Key, out var list))
                {
                    list = new List<string>();
                    result[pair.Key] = list;
                }

                if (pair.Value != null)
                {
                    list.Add(pair.Value);
                }
            }

            return result;
        }

        public async Task<IList<T>> ExecuteSqlAsync<T>(SqlQueryRequest sqlQueryRequest, CancellationToken cancellationToken = default) where T : new()
        {
            var genericResponse = await ExecuteQueryAsync<T>(
                query: sqlQueryRequest.Query,
                parameters: sqlQueryRequest.QueryParameters.ToDictionary(x => x.Key, x => (object)x.Value)
            );

            return genericResponse.Response;
        }
    }
}
