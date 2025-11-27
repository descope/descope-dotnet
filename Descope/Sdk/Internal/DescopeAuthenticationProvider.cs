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
/// and supports optional password/JWT parameters for certain auth operations.
///
/// For management operations: Bearer {projectID}:{managementKey}
/// For auth operations: Bearer {projectID} or Bearer {projectID}:{password/JWT}
///
/// The password or JWT parameter can be passed via additionalAuthenticationContext with keys:
/// - "password": Used for step-up authentication or certain auth methods
/// - "jwt": Used when a valid refresh token is required for the operation
/// </summary>
internal class DescopeAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _projectId;
    private readonly string? _managementKey;
    private readonly bool _isManagementProvider;

    public DescopeAuthenticationProvider(string projectId, string? managementKey = null)
    {
        _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        _managementKey = managementKey;
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
            // Auth client: projectID or projectID:password/jwt
            bearer = _projectId;

            // First check for DescopeJwtOption in request options
            var jwtOption = request.RequestOptions?.OfType<DescopeJwtOption>().FirstOrDefault();
            Dictionary<string, object>? context = jwtOption?.GetContext() ?? additionalAuthenticationContext;

            // Check for password or current JWT in the context
            if (context != null)
            {
                if (context.TryGetValue("jwt", out var token) && token is string jwt && !string.IsNullOrEmpty(jwt))
                {
                    bearer = $"{bearer}:{jwt}";
                }
            }
        }

        request.Headers.Add("Authorization", $"Bearer {bearer}");

        return Task.CompletedTask;
    }
}
