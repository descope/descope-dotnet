using Descope.Mgmt.Models;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Orchestrationv1;
using Descope.Mgmt.Models.Projectv1;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Text.Json;

namespace Descope;

/// <summary>
/// Extension methods for management operations to simplify common patterns.
/// </summary>
public static class MgmtExtensions
{
#pragma warning disable CS0618 // Type or member is obsolete - Allow calling "internally" deprecated Kiota methods
    /// <summary>
    /// Updates password settings by accepting a GetPasswordSettingsResponse and converting it to a ConfigurePasswordSettingsRequest.
    /// This allows for easier modification of existing settings by retrieving current settings and updating specific fields.
    /// </summary>
    /// <param name="requestBuilder">The password settings request builder</param>
    /// <param name="settings">The password settings response to use as the basis for the update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the response</returns>
    public static async Task<Stream?> PostWithSettingsResponseAsync(
        this Descope.Mgmt.V1.Mgmt.Password.Settings.SettingsRequestBuilder requestBuilder,
        GetPasswordSettingsResponse settings,
        CancellationToken cancellationToken = default)
    {
        if (settings == null)
        {
            throw new DescopeException("Settings are required for updating password settings");
        }

        var request = new ConfigurePasswordSettingsRequest
        {
            Enabled = settings.Enabled,
            MinLength = settings.MinLength,
            Lowercase = settings.Lowercase,
            Uppercase = settings.Uppercase,
            Number = settings.Number,
            NonAlphanumeric = settings.NonAlphanumeric,
            EnablePasswordStrength = settings.EnablePasswordStrength,
            PasswordStrengthScore = settings.PasswordStrengthScore,
            Expiration = settings.Expiration,
            ExpirationWeeks = settings.ExpirationWeeks,
            Reuse = settings.Reuse,
            ReuseAmount = settings.ReuseAmount,
            Lock = settings.Lock,
            LockAttempts = settings.LockAttempts,
            TempLock = settings.TempLock,
            TempLockAttempts = settings.TempLockAttempts,
            TempLockDuration = settings.TempLockDuration,
            TenantId = settings.TenantId
        };

        return await requestBuilder.PostAsync(request, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads password settings for a specific tenant by tenant ID.
    /// This is a convenience method that simplifies the common pattern of loading password settings for a tenant.
    /// </summary>
    /// <param name="requestBuilder">The password settings request builder</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>GetPasswordSettingsResponse containing the password settings information</returns>
    public static async Task<GetPasswordSettingsResponse?> GetWithTenantIdAsync(
        this Descope.Mgmt.V1.Mgmt.Password.Settings.SettingsRequestBuilder requestBuilder,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new DescopeException("Tenant ID is required for loading password settings");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.TenantId = tenantId;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads password settings for a the project
    /// This is a convenience method that simplifies the common pattern of loading password settings for the project.
    /// </summary>
    /// <param name="requestBuilder">The password settings request builder</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>GetPasswordSettingsResponse containing the password settings information</returns>
    public static async Task<GetPasswordSettingsResponse?> GetForProjectAsync(
        this Descope.Mgmt.V1.Mgmt.Password.Settings.SettingsRequestBuilder requestBuilder,
        CancellationToken cancellationToken = default)
    {

        return await requestBuilder.GetAsync(requestConfiguration => { }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads a user by identifier (userId or loginId)
    /// This is a convenience method that simplifies the common pattern of loading a user by their identifier.
    /// </summary>
    /// <param name="requestBuilder">The user request builder</param>
    /// <param name="identifier">The user identifier (login ID, email, phone, or user ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>UserResponse containing the user information</returns>
    public static async Task<UserResponse?> GetWithIdentifierAsync(
        this Descope.Mgmt.V1.Mgmt.User.UserRequestBuilder requestBuilder,
        string identifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new DescopeException("Identifier is required for loading a user");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Identifier = identifier;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads an access key by ID.
    /// This is a convenience method that simplifies the common pattern of loading an access key by its ID.
    /// </summary>
    /// <param name="requestBuilder">The access key request builder</param>
    /// <param name="id">The access key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AccessKeyResponse containing the access key information</returns>
    public static async Task<AccessKeyResponse?> GetWithIdAsync(
        this Descope.Mgmt.V1.Mgmt.Accesskey.AccesskeyRequestBuilder requestBuilder,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for loading an access key");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Id = id;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads an SSO application by ID.
    /// This is a convenience method that simplifies the common pattern of loading an SSO application by its ID.
    /// </summary>
    /// <param name="requestBuilder">The SSO app load request builder</param>
    /// <param name="id">The SSO application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadSSOApplicationResponse containing the SSO application information</returns>
    public static async Task<LoadSSOApplicationResponse?> GetWithIdAsync(
        this Descope.Mgmt.V1.Mgmt.Sso.Idp.App.Load.LoadRequestBuilder requestBuilder,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for loading an SSO application");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Id = id;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads SSO settings by tenant ID.
    /// This is a convenience method that simplifies the common pattern of loading SSO settings for a tenant.
    /// </summary>
    /// <param name="requestBuilder">The SSO settings request builder</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadSSOSettingsResponse containing the SSO settings information</returns>
    public static async Task<LoadSSOSettingsResponse?> GetWithTenantIdAsync(
        this Descope.Mgmt.V2.Mgmt.Sso.Settings.SettingsRequestBuilder requestBuilder,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new DescopeException("Tenant ID is required for loading SSO settings");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.TenantId = tenantId;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads a third-party application by ID.
    /// This is a convenience method that simplifies the common pattern of loading a third-party application by its ID.
    /// </summary>
    /// <param name="requestBuilder">The third-party app load request builder</param>
    /// <param name="id">The third-party application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadThirdPartyApplicationResponse containing the third-party application information</returns>
    public static async Task<LoadThirdPartyApplicationResponse?> GetWithIdAsync(
        this Descope.Mgmt.V1.Mgmt.Thirdparty.App.Load.LoadRequestBuilder requestBuilder,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for loading a third-party application");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Id = id;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads a third-party application by client ID.
    /// This is a convenience method that simplifies the common pattern of loading a third-party application by its client ID.
    /// </summary>
    /// <param name="requestBuilder">The third-party app load request builder</param>
    /// <param name="clientId">The third-party application client ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadThirdPartyApplicationResponse containing the third-party application information</returns>
    public static async Task<LoadThirdPartyApplicationResponse?> GetWithClientIdAsync(
        this Descope.Mgmt.V1.Mgmt.Thirdparty.App.Load.LoadRequestBuilder requestBuilder,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            throw new DescopeException("Client ID is required for loading a third-party application");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.ClientId = clientId;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes SSO settings for a tenant by tenant ID.
    /// This is a convenience method that simplifies the common pattern of deleting SSO settings for a tenant.
    /// </summary>
    /// <param name="requestBuilder">The SSO settings request builder</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the response</returns>
    public static async Task<Stream?> DeleteWithTenantIdAsync(
        this Descope.Mgmt.V1.Mgmt.Sso.Settings.SettingsRequestBuilder requestBuilder,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new DescopeException("Tenant ID is required for deleting SSO settings");
        }

        return await requestBuilder.DeleteAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.TenantId = tenantId;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads a tenant by ID.
    /// This is a convenience method that simplifies the common pattern of loading a tenant by its ID.
    /// </summary>
    /// <param name="requestBuilder">The tenant request builder</param>
    /// <param name="id">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadTenantResponse containing the tenant information</returns>
    public static async Task<LoadTenantResponse?> GetWithIdAsync(
        this Descope.Mgmt.V1.Mgmt.Tenant.TenantRequestBuilder requestBuilder,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for loading a tenant");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Id = id;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs a management flow with JSON output deserialization.
    /// This extension method wraps the generated PostAsync method and provides convenient access
    /// to flow output data as a JsonDocument, avoiding the need to manually work with UntypedObject types.
    /// </summary>
    /// <param name="requestBuilder">The run flow request builder</param>
    /// <param name="request">The flow request containing flowId and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FlowResponseWithJson containing the response with JSON-deserialized output</returns>
    /// <example>
    /// <code>
    /// var request = new RunManagementFlowRequest
    /// {
    ///     FlowId = "my-flow",
    ///     Options = new ManagementFlowOptions
    ///     {
    ///         Input = new ManagementFlowOptions_input
    ///         {
    ///             AdditionalData = new Dictionary&lt;string, object&gt;
    ///             {
    ///                 { "email", "user@example.com" }
    ///             }
    ///         }
    ///     }
    /// };
    ///
    /// var response = await client.Mgmt.V1.Flow.Run.PostWithJsonOutputAsync(request);
    ///
    /// // Access output as JSON
    /// var email = response.OutputJson?.Value.GetProperty("email").GetString();
    /// var nested = response.OutputJson?.Value.GetProperty("res").GetProperty("greeting").GetString();
    /// </code>
    /// </example>
    public static async Task<FlowResponseWithJson?> PostWithJsonOutputAsync(
        this Descope.Mgmt.V1.Mgmt.Flow.Run.RunRequestBuilder requestBuilder,
        RunManagementFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new DescopeException("Request is required for running a management flow");
        }

        var response = await requestBuilder.PostAsync(request, cancellationToken: cancellationToken);

        if (response?.Output == null)
        {
            return new FlowResponseWithJson
            {
                Response = response,
                OutputJson = null
            };
        }

        // Deserialize the Output.AdditionalData to JSON
        var outputJson = DeserializeToJson(response.Output);

        return new FlowResponseWithJson
        {
            Response = response,
            OutputJson = outputJson
        };
    }

    /// <summary>
    /// Deserializes an IAdditionalDataHolder's AdditionalData to a JsonElement.
    /// This helper method converts Kiota's UntypedObject/UntypedString types to a standard JsonElement.
    /// The JsonDocument is disposed internally, and the cloned JsonElement is safe to use.
    /// </summary>
    private static JsonElement? DeserializeToJson(IAdditionalDataHolder additionalDataHolder)
    {
        if (additionalDataHolder?.AdditionalData == null || additionalDataHolder.AdditionalData.Count == 0)
        {
            return null;
        }

        // Create a temporary UntypedObject wrapper with the AdditionalData
        var wrapper = new RunManagementFlowResponse_output
        {
            AdditionalData = additionalDataHolder.AdditionalData
        };

        // Serialize the wrapper to JSON string
        var serializedValue = KiotaJsonSerializer.SerializeAsString(wrapper);

        // Parse as JsonDocument, clone the root element, then dispose the document
        using var document = JsonDocument.Parse(serializedValue);
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Creates an Agentic Governance Suite access request to provision an agent client on behalf of a developer.
    /// The requestor must be an existing, active console user in your company. When auto-approval is on and the
    /// current policies already permit the request, it is approved and the client provisioned immediately.
    /// </summary>
    /// <param name="requestBuilder">The governance access request create request builder</param>
    /// <param name="requestorUserId">The console user the request is raised for. Overrides <c>request.RequestorUserId</c>.</param>
    /// <param name="request">The access request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created access request</returns>
    /// <example>
    /// <code>
    /// var created = await client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithRequestorUserIdAsync(
    ///     "requestor-user-id",
    ///     new CreateGovernanceAccessRequestRequest
    ///     {
    ///         ClientName = "My agent",
    ///         GrantTypes = new List&lt;string&gt; { "clientCredentials" },
    ///         Targets = new List&lt;GovernanceAccessRequestTarget&gt;
    ///         {
    ///             new GovernanceAccessRequestTarget { Name = "my-mcp-server", Kind = "resource", AllScopes = true }
    ///         },
    ///         Justification = "Needed for the nightly sync",
    ///     });
    /// </code>
    /// </example>
    public static async Task<GovernanceAccessRequest?> PostWithRequestorUserIdAsync(
        this Descope.Mgmt.V1.Mgmt.Agentic.Governance.Requests.Create.CreateRequestBuilder requestBuilder,
        string requestorUserId,
        CreateGovernanceAccessRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(requestorUserId))
        {
            throw new DescopeException("Requestor user ID is required for creating a governance access request");
        }
        if (request == null)
        {
            throw new DescopeException("Request is required for creating a governance access request");
        }
        if (request.GrantTypes != null && request.GrantTypes.Any(string.IsNullOrEmpty))
        {
            throw new DescopeException("Grant types must not contain empty values when creating a governance access request");
        }

        request.RequestorUserId = requestorUserId;
        return await requestBuilder.PostAsync(request, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads a single Agentic Governance Suite access request by ID.
    /// </summary>
    /// <param name="requestBuilder">The governance access request get request builder</param>
    /// <param name="id">The access request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The access request</returns>
    /// <example>
    /// <code>
    /// var accessRequest = await client.Mgmt.V1.Agentic.Governance.Requests.GetPath.PostWithIdAsync("request-id");
    /// </code>
    /// </example>
    public static async Task<GovernanceAccessRequest?> PostWithIdAsync(
        this Descope.Mgmt.V1.Mgmt.Agentic.Governance.Requests.Get.GetRequestBuilder requestBuilder,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for loading a governance access request");
        }

        return await requestBuilder.PostAsync(new GetGovernanceAccessRequestRequest { Id = id }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Approves or denies a pending Agentic Governance Suite access request. Approving provisions the requested
    /// agent client. The resolution is attributed to the Management API.
    /// </summary>
    /// <param name="requestBuilder">The governance access request resolve request builder</param>
    /// <param name="id">The access request ID</param>
    /// <param name="action">"approve" or "deny"</param>
    /// <param name="resolutionNote">Optional note recorded with the resolution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resolved access request</returns>
    /// <example>
    /// <code>
    /// var resolved = await client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithIdAsync("request-id", "approve", "Approved for the pilot");
    /// </code>
    /// </example>
    public static async Task<GovernanceAccessRequest?> PostWithIdAsync(
        this Descope.Mgmt.V1.Mgmt.Agentic.Governance.Requests.Resolve.ResolveRequestBuilder requestBuilder,
        string id,
        string action,
        string? resolutionNote = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for resolving a governance access request");
        }
        if (string.IsNullOrEmpty(action))
        {
            throw new DescopeException("Action is required for resolving a governance access request");
        }

        var request = new ResolveGovernanceAccessRequestRequest
        {
            Id = id,
            Action = action,
            ResolutionNote = resolutionNote,
        };
        return await requestBuilder.PostAsync(request, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Searches the project's Agentic Governance Suite access requests, with filtering, sorting and paging.
    /// Validates the request before sending it.
    /// </summary>
    /// <param name="requestBuilder">The governance access requests search request builder</param>
    /// <param name="request">The search request. Every sort entry must name a field.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching access requests and their total count</returns>
    /// <example>
    /// <code>
    /// var result = await client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(
    ///     new SearchGovernanceAccessRequestsRequest
    ///     {
    ///         Limit = 10,
    ///         Sort = new List&lt;SortField&gt; { new SortField { Field = "createdTime", Desc = true } },
    ///     });
    /// </code>
    /// </example>
    public static async Task<SearchGovernanceAccessRequestsResponse?> PostWithValidationAsync(
        this Descope.Mgmt.V1.Mgmt.Agentic.Governance.Requests.Search.SearchRequestBuilder requestBuilder,
        SearchGovernanceAccessRequestsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new DescopeException("Request is required for searching governance access requests");
        }
        if (request.Sort != null && request.Sort.Any(s => s == null || string.IsNullOrEmpty(s.Field)))
        {
            throw new DescopeException("Every sort entry must have a field when searching governance access requests");
        }

        return await requestBuilder.PostAsync(request, cancellationToken: cancellationToken);
    }
#pragma warning restore CS0618
}

/// <summary>
/// Response wrapper that includes both the original RunManagementFlowResponse and
/// a deserialized JSON representation of the output for easier access.
/// </summary>
public class FlowResponseWithJson
{
    /// <summary>
    /// The original response from the flow execution.
    /// </summary>
    public RunManagementFlowResponse? Response { get; set; }

    /// <summary>
    /// The flow output deserialized as a JsonElement for easier property access.
    /// Access nested properties using standard JsonElement methods:
    /// <code>
    /// var email = response.OutputJson?.Value.GetProperty("email").GetString();
    /// </code>
    /// </summary>
    public JsonElement? OutputJson { get; set; }
}
