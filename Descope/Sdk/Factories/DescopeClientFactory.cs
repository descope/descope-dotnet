using Descope.Mgmt;
using Descope.Auth;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;

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

        // Set BaseUrl if not explicitly provided, using region-based logic
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            options.BaseUrl = DescopeClientOptions.GetBaseUrlForProjectId(options.ProjectId);
        }

        // Create separate authentication providers for management and auth
        var mgmtAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey);
        var authAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, null, options.AuthManagementKey);

        // Create the base handler
        HttpClientHandler baseHandler = new HttpClientHandler
        {
#if NETSTANDARD2_0
            ServerCertificateCustomValidationCallback = options.IsUnsafe
                ? (message, cert, chain, errors) => true
                : null
#else
            ServerCertificateCustomValidationCallback = options.IsUnsafe
                ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                : null
#endif
        };

        // Build the handler pipeline: Base -> FGA Cache -> OpenAPI Fix -> Error Handler
        var fgaCacheHandler = new Internal.FgaCacheUrlHandler(options.FgaCacheUrl)
        {
            InnerHandler = baseHandler
        };

        var openApiFixHandler = new Internal.FixRootResponseBodyHandler
        {
            InnerHandler = fgaCacheHandler
        };

        var errorHandler = new DescopeErrorResponseHandler
        {
            InnerHandler = openApiFixHandler
        };

        // Create HttpClient instances
        // Separate HttpClient instances are needed to avoid rare race conditions when making both management and auth calls concurrently
        HttpClient mgmtHttpClient = CreateDescopeHttpClient(options.ProjectId, errorHandler);
        HttpClient authHttpClient = CreateDescopeHttpClient(options.ProjectId, errorHandler);
        HttpClient fetchKeysHttpClient = CreateDescopeHttpClient(options.ProjectId, errorHandler);

        // Create separate request adapters for management and auth
        var mgmtAdapter = new HttpClientRequestAdapter(mgmtAuthProvider, httpClient: mgmtHttpClient)
        {
            BaseUrl = options.BaseUrl
        };

        var authAdapter = new HttpClientRequestAdapter(authAuthProvider, httpClient: authHttpClient)
        {
            BaseUrl = options.BaseUrl
        };

        // Create both generated clients with their respective adapters
        var mgmtClient = new DescopeMgmtKiotaClient(mgmtAdapter);
        var authClient = new DescopeAuthKiotaClient(authAdapter);

        // Wrap both clients with JWT validation support
        return new DescopeClient(mgmtClient, authClient, options.ProjectId, options.BaseUrl!, fetchKeysHttpClient);
    }

    private static HttpClient CreateDescopeHttpClient(string projectId, DescopeErrorResponseHandler errorHandler)
    {
        HttpClient httpClient = new(errorHandler);
        DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId);
        return httpClient;
    }

    /// <summary>
    /// Creates a new instance of the Descope Client for testing purposes using mock request adapters.
    /// This method allows injecting custom request adapters to avoid making real HTTP calls during testing.
    /// </summary>
    /// <param name="authAdapter">The request adapter for auth endpoints.</param>
    /// <param name="mgmtAdapter">The request adapter for management endpoints.</param>
    /// <param name="options">The configuration options for the client.</param>
    /// <param name="httpClient">Optional HttpClient for HTTP-level mocking (e.g., error responses). If provided, enables JWT validation.</param>
    /// <returns>A new instance of the Descope Client configured with the provided adapters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="DescopeException">Thrown when required options are not set.</exception>
    public static IDescopeClient CreateForTest(
        IRequestAdapter authAdapter,
        IRequestAdapter mgmtAdapter,
        DescopeClientOptions options,
        HttpClient httpClient)
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

        // Set BaseUrl if not explicitly provided, using region-based logic
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            options.BaseUrl = DescopeClientOptions.GetBaseUrlForProjectId(options.ProjectId);
        }

        // Create the Kiota clients with the provided adapters
        var authKiotaClient = new DescopeAuthKiotaClient(authAdapter);
        var mgmtKiotaClient = new DescopeMgmtKiotaClient(mgmtAdapter);

        // Create and return the wrapper client with optional HttpClient for HTTP-level testing
        return new DescopeClient(mgmtKiotaClient, authKiotaClient, options.ProjectId, options.BaseUrl!, httpClient);
    }
}
