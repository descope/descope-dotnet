using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Descope;

/// <summary>
/// Custom authentication provider for Descope that supports dynamic bearer token generation.
/// This provider allows for different authentication strategies between management and auth endpoints,
/// and supports optional password/JWT parameters for certain auth operations.
///
/// For management operations: Bearer {projectID}:{managementKey}
/// For auth operations: Bearer {projectID} or Bearer {projectID}:{password/JWT}
///
/// The password or JWT parameter can be passed via additionalAuthenticationContext with keys:
/// - "password": Used for step-up authentication or certain auth methods
/// - "jwt": Used when a valid refresh token is required for the operation
/// </summary>
public class DescopeAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _projectId;
    private readonly string? _managementKey;
    private readonly bool _isManagementProvider;

    /// <summary>
    /// Creates a new instance of DescopeAuthenticationProvider for management operations.
    /// </summary>
    /// <param name="projectId">The Descope Project ID.</param>
    /// <param name="managementKey">The Descope Management Key.</param>
    public DescopeAuthenticationProvider(string projectId, string managementKey)
    {
        _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        _managementKey = managementKey ?? throw new ArgumentNullException(nameof(managementKey));
        _isManagementProvider = true;
    }

    /// <summary>
    /// Creates a new instance of DescopeAuthenticationProvider for auth operations.
    /// </summary>
    /// <param name="projectId">The Descope Project ID.</param>
    public DescopeAuthenticationProvider(string projectId)
    {
        _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        _managementKey = null;
        _isManagementProvider = false;
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
            // Auth client: projectID or projectID:password
            bearer = _projectId;

            // Check for password or current JWT in additional context
            // The password parameter may be passed from certain auth methods
            if (additionalAuthenticationContext != null)
            {
                if (additionalAuthenticationContext.TryGetValue("password", out var pswd) && pswd is string password && !string.IsNullOrEmpty(password))
                {
                    bearer = $"{bearer}:{password}";
                }
                else if (additionalAuthenticationContext.TryGetValue("jwt", out var token) && token is string jwt && !string.IsNullOrEmpty(jwt))
                {
                    bearer = $"{bearer}:{jwt}";
                }
            }
        }

        request.Headers.Add("Authorization", $"Bearer {bearer}");

        return Task.CompletedTask;
    }
}
