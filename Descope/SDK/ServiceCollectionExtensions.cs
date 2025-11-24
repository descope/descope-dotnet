using Descope.Mgmt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Descope;

/// <summary>
/// Extension methods for registering Descope Management Client with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Descope Management Client to the service collection using HttpClientFactory.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="projectId">The Descope Project ID (required).</param>
    /// <param name="managementKey">The Descope Management Key (required).</param>
    /// <param name="httpClientFactoryName">The name of the HttpClient to use from IHttpClientFactory (required).</param>
    /// <param name="baseUrl">The base URL for the Descope API. If null, defaults to "https://api.descope.com".</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null or empty.</exception>
    public static IServiceCollection AddDescopeManagementClient(
        this IServiceCollection services,
        string projectId,
        string managementKey,
        string httpClientFactoryName,
        string? baseUrl = null)
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
            HttpClientFactoryName = httpClientFactoryName
        };

        return services.AddDescopeManagementClient(options);
    }

    /// <summary>
    /// Adds the Descope Management Client to the service collection using HttpClientFactory with options.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="options">The configuration options for the client.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when required options are not set.</exception>
    public static IServiceCollection AddDescopeManagementClient(
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

        // Register the authentication provider
        services.AddSingleton<IAuthenticationProvider>(sp =>
        {
            var token = $"{options.ProjectId}:{options.ManagementKey}";
            return new ApiKeyAuthenticationProvider(
                $"Bearer {token}",
                "Authorization",
                ApiKeyAuthenticationProvider.KeyLocation.Header);
        });

        // Register the generated Kiota client
        services.AddScoped<DescopeMgmtKiotaClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var authProvider = sp.GetRequiredService<IAuthenticationProvider>();

            var httpClient = httpClientFactory.CreateClient(options.HttpClientFactoryName!);
            var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
            {
                BaseUrl = options.BaseUrl
            };

            return new DescopeMgmtKiotaClient(adapter);
        });

        // Register the wrapper client
        services.AddScoped<IDescopeManagementClient, DescopeManagementClient>();

        return services;
    }

    /// <summary>
    /// Adds the Descope Management Client to the service collection using a configuration delegate.
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

        return services.AddDescopeManagementClient(options);
    }
}
