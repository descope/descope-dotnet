using Descope.Mgmt;
using Descope.Auth;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net.Http;

namespace Descope;

/// <summary>
/// Factory for creating instance-based Descope Clients without dependency injection.
/// This approach creates and manages its own HttpClient instance.
/// </summary>
public static class DescopeManagementClientFactory
{
    /// <summary>
    /// Creates a new instance of the Descope Client using options.
    /// </summary>
    /// <param name="options">The configuration options for the client.</param>
    /// <returns>A new instance of the Descope Client.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public static IDescopeClient Create(DescopeClientOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.Validate();

        // Create separate authentication providers for management and auth
        var mgmtAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey);
        var authAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, null);

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

        // Configure Descope headers
        DescopeHttpClientHandler.ConfigureHeaders(httpClient, options.ProjectId);

        // Create separate request adapters for management and auth
        var mgmtAdapter = new HttpClientRequestAdapter(mgmtAuthProvider, httpClient: httpClient)
        {
            BaseUrl = options.BaseUrl
        };

        var authAdapter = new HttpClientRequestAdapter(authAuthProvider, httpClient: httpClient)
        {
            BaseUrl = options.BaseUrl
        };

        // Create both generated clients with their respective adapters
        var mgmtClient = new DescopeMgmtKiotaClient(mgmtAdapter);
        var authClient = new DescopeAuthKiotaClient(authAdapter);

        // Wrap both clients with JWT validation support
        return new DescopeClient(mgmtClient, authClient, options.ProjectId, options.BaseUrl, httpClient);
    }

    /// <summary>
    /// Creates a new instance of the Descope Client for testing purposes using mock request adapters.
    /// This method allows injecting custom request adapters to avoid making real HTTP calls during testing.
    /// </summary>
    /// <param name="authAdapter">The request adapter for auth endpoints.</param>
    /// <param name="mgmtAdapter">The request adapter for management endpoints.</param>
    /// <param name="options">The configuration options for the client.</param>
    /// <returns>A new instance of the Descope Client configured with the provided adapters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="DescopeException">Thrown when required options are not set.</exception>
    public static IDescopeClient CreateForTest(
        IRequestAdapter authAdapter,
        IRequestAdapter mgmtAdapter,
        DescopeClientOptions options)
    {
        if (authAdapter == null)
        {
            throw new ArgumentNullException(nameof(authAdapter));
        }

        if (mgmtAdapter == null)
        {
            throw new ArgumentNullException(nameof(mgmtAdapter));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.Validate();

        // Create the Kiota clients with the provided adapters
        var authKiotaClient = new DescopeAuthKiotaClient(authAdapter);
        var mgmtKiotaClient = new DescopeMgmtKiotaClient(mgmtAdapter);

        // Create and return the wrapper client
        return new DescopeClient(mgmtKiotaClient, authKiotaClient, options.ProjectId, options.BaseUrl);
    }
}
