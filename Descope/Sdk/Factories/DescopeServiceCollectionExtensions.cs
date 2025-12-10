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

        // Add the open api application response fix handler to the pipeline
        httpClientBuilder.AddHttpMessageHandler(() => new Internal.FixRootResponseBodyHandler());

        // Add the error response handler to the pipeline (outermost handler)
        httpClientBuilder.AddHttpMessageHandler(() => new DescopeErrorResponseHandler());

        if (options.IsUnsafe)
        {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
            {
#if NETSTANDARD2_0
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#else
                ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
#endif
            });
        }

        // Create and register the Descope Client Service
        services.AddScoped<IDescopeClient>(sp =>
        {
            // Create HttpClient from factory
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(httpClientName);

            // Create management Kiota client
            var mgmtAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey);
            DescopeHttpHeaders.ConfigureHeaders(httpClient, options.ProjectId);
            var mgmtAdapter = new HttpClientRequestAdapter(mgmtAuthProvider, httpClient: httpClient)
            {
                BaseUrl = options.BaseUrl
            };
            var mgmtClient = new DescopeMgmtKiotaClient(mgmtAdapter);

            // Create auth Kiota client
            var authAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, null, options.AuthManagementKey);
            DescopeHttpHeaders.ConfigureHeaders(httpClient, options.ProjectId);
            var authAdapter = new HttpClientRequestAdapter(authAuthProvider, httpClient: httpClient)
            {
                BaseUrl = options.BaseUrl
            };
            var authClient = new DescopeAuthKiotaClient(authAdapter);

            // Create the wrapper client with internal Kiota clients
            return new DescopeClient(mgmtClient, authClient, options.ProjectId, options.BaseUrl!, httpClient);
        });

        return services;
    }
}
