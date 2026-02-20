using Descope.Mgmt;
using Descope.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Descope;

/// <summary>
/// Extension methods for registering Descope Client with dependency injection.
/// </summary>
public static class DescopeServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Descope Client to the service collection using HttpClientFactory with options.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="options">The configuration options for the client.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="DescopeException">Thrown when required options are not set.</exception>
    public static IServiceCollection AddDescopeClient(
        this IServiceCollection services,
        DescopeClientOptions options)
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

        // Use a default HttpClient factory name if not provided
        var httpClientName = string.IsNullOrWhiteSpace(options.HttpClientFactoryName)
            ? "DescopeClient"
            : options.HttpClientFactoryName!;

        // Configure HttpClient with handlers and optional unsafe SSL handling
        var httpClientBuilder = services.AddHttpClient(httpClientName);

        // Add the cookie-to-body handler to the pipeline (innermost middleware)
        // Extracts JWTs from Set-Cookie headers into the response body for "Manage in cookies" mode
        httpClientBuilder.AddHttpMessageHandler(() => new Internal.CookieToBodyHandler());

        // Add the FGA cache URL handler to the pipeline
        httpClientBuilder.AddHttpMessageHandler(() => new Internal.FgaCacheUrlHandler(options.FgaCacheUrl));

        // Add the open api application response fix handler to the pipeline
        httpClientBuilder.AddHttpMessageHandler(() => new Internal.FixRootResponseBodyHandler());

        // Add the error response handler to the pipeline (outermost handler)
        httpClientBuilder.AddHttpMessageHandler(() => new DescopeErrorResponseHandler());

        // Always configure the primary handler to disable automatic cookie handling.
        // Cookies must never be implicitly shared between requests — the SDK reads
        // cookies from individual HTTP responses via the CookieToBodyHandler instead.
        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new System.Net.Http.HttpClientHandler
            {
                UseCookies = false
            };
            if (options.IsUnsafe)
            {
#if NETSTANDARD2_0
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#else
                handler.ServerCertificateCustomValidationCallback =
                    System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif
            }
            return handler;
        });

        // Create and register the Descope Client Service
        services.AddScoped<IDescopeClient>(sp =>
        {
            // Create HttpClients from factory
            // Separate HttpClient instances are needed to avoid cross contamination of http handlers, which most likely happens due to internal Kiota implementation details
            HttpClient mgmtHttpClient = CreateDescopeHttpClientFromFactory(options.ProjectId, sp, httpClientName);
            HttpClient authHttpClient = CreateDescopeHttpClientFromFactory(options.ProjectId, sp, httpClientName);
            HttpClient fetchKeysHttpClient = CreateDescopeHttpClientFromFactory(options.ProjectId, sp, httpClientName);

            // Create management Kiota client
            var mgmtAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey);
            var mgmtAdapter = new HttpClientRequestAdapter(mgmtAuthProvider, httpClient: mgmtHttpClient)
            {
                BaseUrl = options.BaseUrl
            };
            var mgmtClient = new DescopeMgmtKiotaClient(mgmtAdapter);

            // Create auth Kiota client
            var authAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, null, options.AuthManagementKey);
            var authAdapter = new HttpClientRequestAdapter(authAuthProvider, httpClient: authHttpClient)
            {
                BaseUrl = options.BaseUrl
            };
            var authClient = new DescopeAuthKiotaClient(authAdapter);

            // Create the wrapper client with internal Kiota clients
            return new DescopeClient(mgmtClient, authClient, options.ProjectId, options.BaseUrl!, fetchKeysHttpClient);
        });

        return services;
    }

    private static HttpClient CreateDescopeHttpClientFromFactory(string projectId, IServiceProvider sp, string httpClientName)
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(httpClientName);
        DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId);
        return httpClient;
    }
}
