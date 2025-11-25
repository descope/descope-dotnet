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
    /// Adds the Descope Client to the service collection using HttpClientFactory.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="projectId">The Descope Project ID (required).</param>
    /// <param name="managementKey">The Descope Management Key (required).</param>
    /// <param name="httpClientFactoryName">The name of the HttpClient to use from IHttpClientFactory (required).</param>
    /// <param name="baseUrl">The base URL for the Descope API. If null, defaults to "https://api.descope.com".</param>
    /// <param name="isUnsafe">If true, allows unsafe HTTPS calls by accepting any server certificate. WARNING: Only use for development/testing.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null or empty.</exception>
    public static IServiceCollection AddDescopeClient(
        this IServiceCollection services,
        string projectId,
        string managementKey,
        string httpClientFactoryName,
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

        if (string.IsNullOrWhiteSpace(httpClientFactoryName))
        {
            throw new ArgumentException("HttpClient factory name is required", nameof(httpClientFactoryName));
        }

        var options = new DescopeManagementClientOptions
        {
            ProjectId = projectId,
            ManagementKey = managementKey,
            BaseUrl = baseUrl ?? "https://api.descope.com",
            HttpClientFactoryName = httpClientFactoryName,
            IsUnsafe = isUnsafe
        };

        return services.AddDescopeClient(options);
    }

    /// <summary>
    /// Adds the Descope Client to the service collection using HttpClientFactory with options.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="options">The configuration options for the client.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required options are not set.</exception>
    public static IServiceCollection AddDescopeClient(
        this IServiceCollection services,
        DescopeManagementClientOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.Validate();

        if (string.IsNullOrWhiteSpace(options.HttpClientFactoryName))
        {
            throw new ArgumentException("HttpClientFactoryName is required for DI registration", nameof(options.HttpClientFactoryName));
        }

        // Configure HttpClient with optional unsafe SSL handling
        var httpClientBuilder = services.AddHttpClient(options.HttpClientFactoryName!);
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

        // Register separate authentication providers for management and auth
        services.AddSingleton<IAuthenticationProvider>(sp =>
            new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey));

        services.AddSingleton<IAuthenticationProvider>(sp =>
            new DescopeAuthenticationProvider(options.ProjectId));

        // Register the generated Kiota clients
        services.AddScoped<DescopeMgmtKiotaClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            // Get the management auth provider (first one registered)
            var mgmtAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey);

            var httpClient = httpClientFactory.CreateClient(options.HttpClientFactoryName!);

            // Configure Descope headers
            DescopeHttpClientHandler.ConfigureHeaders(httpClient, options.ProjectId);

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
            var authAuthProvider = new DescopeAuthenticationProvider(options.ProjectId);

            var httpClient = httpClientFactory.CreateClient(options.HttpClientFactoryName!);

            // Configure Descope headers
            DescopeHttpClientHandler.ConfigureHeaders(httpClient, options.ProjectId);

            var adapter = new HttpClientRequestAdapter(authAuthProvider, httpClient: httpClient)
            {
                BaseUrl = options.BaseUrl
            };

            return new DescopeAuthKiotaClient(adapter);
        });

        // Register the wrapper client
        services.AddScoped<IDescopeClient, DescopeClient>();

        return services;
    }

    /// <summary>
    /// Adds the Descope Client to the service collection using a configuration delegate.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="configureOptions">A delegate to configure the client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDescopeManagementClient(
        this IServiceCollection services,
        Action<DescopeManagementClientOptions> configureOptions)
    {
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        var options = new DescopeManagementClientOptions();
        configureOptions(options);

        return services.AddDescopeClient(options);
    }
}
