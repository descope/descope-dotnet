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

        if (string.IsNullOrWhiteSpace(options.HttpClientFactoryName))
        {
            throw new DescopeException("HttpClientFactoryName is required for DI registration");
        }

        // Configure HttpClient with optional unsafe SSL handling and error handling
        var httpClientBuilder = services.AddHttpClient(options.HttpClientFactoryName!);

        // Add the open api application response fix handler to the pipeline
        httpClientBuilder.AddHttpMessageHandler(() => new Internal.FixRootResponseBodyHandler());

        // Add the error response handler to the pipeline
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

        // Register authentication provider
        services.AddSingleton<IAuthenticationProvider>(sp =>
            new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey));

        // Register the generated Kiota clients
        services.AddScoped<DescopeMgmtKiotaClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            // Get the management auth provider (first one registered)
            var mgmtAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey);

            var httpClient = httpClientFactory.CreateClient(options.HttpClientFactoryName!);

            // Configure Descope headers
            DescopeHttpHeaders.ConfigureHeaders(httpClient, options.ProjectId);

            var adapter = new HttpClientRequestAdapter(mgmtAuthProvider, httpClient: httpClient)
            {
                BaseUrl = options.BaseUrl
            };

            return new DescopeMgmtKiotaClient(adapter);
        });

        services.AddScoped<DescopeAuthKiotaClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            // Get the auth provider (project ID only)
            var authAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, null);

            var httpClient = httpClientFactory.CreateClient(options.HttpClientFactoryName!);

            // Configure Descope headers
            DescopeHttpHeaders.ConfigureHeaders(httpClient, options.ProjectId);

            var adapter = new HttpClientRequestAdapter(authAuthProvider, httpClient: httpClient)
            {
                BaseUrl = options.BaseUrl
            };

            return new DescopeAuthKiotaClient(adapter);
        });

        // Register the wrapper client
        services.AddScoped<IDescopeClient>(sp =>
        {
            var mgmtClient = sp.GetRequiredService<DescopeMgmtKiotaClient>();
            var authClient = sp.GetRequiredService<DescopeAuthKiotaClient>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(options.HttpClientFactoryName!);

            return new DescopeClient(mgmtClient, authClient, options.ProjectId, options.BaseUrl, httpClient);
        });

        return services;
    }
}
