using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Descope;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Descope;

/// <summary>
/// Custom authentication provider for Descope that supports dynamic bearer token generation.
/// This provider allows for different authentication strategies between management and auth endpoints,
/// and supports optional JWT/key parameters for certain auth operations.
///
/// For management operations: Bearer {projectID}:{managementKey}
/// For auth operations:
///   - Bearer {projectID}
///   - Bearer {projectID}:{refreshJWT}
///   - Bearer {projectID}:{authManagementKey}
///   - Bearer {projectID}:{refreshJWT}:{authManagementKey}
///   - Bearer {projectID}:{accessKey} (authManagementKey is NOT appended for access keys)
///
/// The JWT or key parameter can be passed via request options:
/// - DescopeJwtOption: Used when a valid refresh token is required for the operation
/// - DescopeKeyOption: Used for access key authentication (does not append authManagementKey)
/// </summary>
internal class DescopeAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _projectId;
    private readonly string? _managementKey;
    private readonly string? _authManagementKey;
    private readonly bool _isManagementProvider;

    public DescopeAuthenticationProvider(string projectId, string? managementKey = null, string? authManagementKey = null)
    {
        _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        _managementKey = managementKey;
        _authManagementKey = authManagementKey;
        _isManagementProvider = !string.IsNullOrWhiteSpace(managementKey);
    }

    /// <inheritdoc/>
    public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string bearer;

        if (_isManagementProvider)
        {
            // Management client: projectID:managementKey
            bearer = $"{_projectId}:{_managementKey}";
        }
        else
        {
            // Auth client: projectID or projectID:jwt or projectID:authManagementKey or projectID:jwt:authManagementKey or projectID:key
            // projectID
            bearer = _projectId;

            bool isKeyAuth = false;

            // JWT or Key
            // Check for DescopeKeyOption first (access key authentication)
            var keyOption = request.RequestOptions?.OfType<DescopeKeyOption>().FirstOrDefault();
            if (keyOption != null)
            {
                var keyContext = keyOption.GetContext();
                if (keyContext != null && keyContext.TryGetValue("key", out var keyToken) && keyToken is string key && !string.IsNullOrEmpty(key))
                {
                    bearer = $"{bearer}:{key}";
                    isKeyAuth = true; // Mark as key authentication
                }
            }
            else
            {
                // Check for DescopeJwtOption (JWT authentication)
                var jwtOption = request.RequestOptions?.OfType<DescopeJwtOption>().FirstOrDefault();
                Dictionary<string, object>? context = jwtOption?.GetContext() ?? additionalAuthenticationContext;
                if (context != null)
                {
                    if (context.TryGetValue("jwt", out var token) && token is string jwt && !string.IsNullOrEmpty(jwt))
                    {
                        bearer = $"{bearer}:{jwt}";
                    }
                }
            }

            // AuthManagementKey
            // Only append authManagementKey if NOT using key authentication
            if (!isKeyAuth && !string.IsNullOrWhiteSpace(_authManagementKey))
            {
                bearer = $"{bearer}:{_authManagementKey}";
            }
        }

        request.Headers.Add("Authorization", $"Bearer {bearer}");

        return Task.CompletedTask;
    }
}
