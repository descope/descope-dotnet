using Descope.Mgmt;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net.Http;

namespace Descope;

/// <summary>
/// Factory for creating instance-based Descope Management Clients without dependency injection.
/// This approach creates and manages its own HttpClient instance.
/// </summary>
public static class DescopeManagementClientFactory
{
    /// <summary>
    /// Creates a new instance of the Descope Management Client.
    /// </summary>
    /// <param name="projectId">The Descope Project ID (required).</param>
    /// <param name="managementKey">The Descope Management Key (required).</param>
    /// <param name="baseUrl">The base URL for the Descope API. If null, defaults to "https://api.descope.com".</param>
    /// <param name="isUnsafe">
    /// If true, allows unsafe HTTPS calls by accepting any server certificate.
    /// WARNING: Only use this for development/testing purposes. Default is false.
    /// </param>
    /// <returns>A new instance of the Descope Management Client.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are null or empty.</exception>
    public static IDescopeManagementClient Create(
        string projectId,
        string managementKey,
        string? baseUrl = null,
        bool isUnsafe = false)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("Project ID is required", nameof(projectId));
        }

        if (string.IsNullOrWhiteSpace(managementKey))
        {
            throw new ArgumentException("Management Key is required", nameof(managementKey));
        }

        var options = new DescopeManagementClientOptions
        {
            ProjectId = projectId,
            ManagementKey = managementKey,
            BaseUrl = baseUrl ?? "https://api.descope.com",
            IsUnsafe = isUnsafe
        };

        return Create(options);
    }

    /// <summary>
    /// Creates a new instance of the Descope Management Client using options.
    /// </summary>
    /// <param name="options">The configuration options for the client.</param>
    /// <returns>A new instance of the Descope Management Client.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required options are not set.</exception>
    public static IDescopeManagementClient Create(DescopeManagementClientOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.Validate();

        // Create authentication provider
        var token = $"{options.ProjectId}:{options.ManagementKey}";
        var authProvider = new ApiKeyAuthenticationProvider(
            $"Bearer {token}",
            "Authorization",
            ApiKeyAuthenticationProvider.KeyLocation.Header);

        // Create HttpClient with optional unsafe SSL handling
        HttpClient httpClient;
        if (options.IsUnsafe)
        {
            var handler = new HttpClientHandler
            {
#if NETSTANDARD2_0
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#else
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
#endif
            };
            httpClient = new HttpClient(handler);
        }
        else
        {
            httpClient = new HttpClient();
        }

        // Create request adapter
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = options.BaseUrl
        };

        // Create and wrap the generated client
        var generatedClient = new DescopeMgmtKiotaClient(adapter);
        return new DescopeManagementClient(generatedClient);
    }

    /// <summary>
    /// Creates a new instance of the Descope Management Client using a configuration delegate.
    /// </summary>
    /// <param name="configureOptions">A delegate to configure the client options.</param>
    /// <returns>A new instance of the Descope Management Client.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configureOptions is null.</exception>
    public static IDescopeManagementClient Create(Action<DescopeManagementClientOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        var options = new DescopeManagementClientOptions();
        configureOptions(options);

        return Create(options);
    }
}
