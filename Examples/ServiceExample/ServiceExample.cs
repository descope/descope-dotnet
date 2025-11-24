using Descope;
using Descope.Mgmt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Configuration model for reading from config.json
/// </summary>
public class Config
{
    public string ProjectId { get; set; } = string.Empty;
    public string ManagementKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool Unsafe { get; set; }
}

/// <summary>
/// Example demonstrating how to use the Descope Management Client with dependency injection.
/// This example shows the recommended approach using HttpClientFactory, logging, and Polly policies.
/// </summary>
public class ServiceExample
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Read configuration from config.json
            var configPath = Path.Combine("..", "config.json");
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"ERROR: Configuration file not found at {Path.GetFullPath(configPath)}");
                Console.WriteLine("Please create a config.json file in the Examples directory.");
                return;
            }

            var configJson = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<Config>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config == null)
            {
                Console.WriteLine("ERROR: Failed to parse configuration file.");
                return;
            }

            // Use default base URL if not specified
            var baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl) ? "https://api.descope.com" : config.BaseUrl;

            // Configure services with DI container
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Configure HttpClient with HttpClientFactory, Polly policies, and custom handlers
            var httpClientBuilder = services.AddHttpClient("DescopeClient");

            // Only configure unsafe SSL handling if specified in config
            if (config.Unsafe)
            {
                httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    // Only for development/testing - accept any SSL certificate
#if NETSTANDARD2_0
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#else
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
#endif
                });
            }

            httpClientBuilder
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Register Descope Management Client using the extension method
            services.AddDescopeManagementClient(
                projectId: config.ProjectId,
                managementKey: config.ManagementKey,
                httpClientFactoryName: "DescopeClient",
                baseUrl: baseUrl);

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            try
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ServiceExample>>();
                var client = serviceProvider.GetRequiredService<IDescopeManagementClient>();

                logger.LogInformation("Starting user search using DI-based client...");

                // Search for 2 users
                var usersResponse = await client.V2.Mgmt.User.Search.PostAsync(
                    new Descope.Mgmt.Models.Managementv1.SearchUsersRequest
                    {
                        Limit = 2
                    });

                logger.LogInformation("Retrieved {UserCount} users", usersResponse?.Total);
                Console.WriteLine($"Successfully retrieved {usersResponse?.Total} users.");

                // Display user information
                if (usersResponse?.Users != null)
                {
                    foreach (var user in usersResponse.Users)
                    {
                        Console.WriteLine($"  - User ID: {user.UserId}, Login IDs: {string.Join(", ", user.LoginIds ?? new System.Collections.Generic.List<string>())}");
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ServiceExample>>();
                logger.LogError(ex, "An error occurred while searching users");
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            finally
            {
                // Dispose service provider to clean up resources
                if (serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    // Retry policy with exponential backoff
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
#if !NETSTANDARD2_0
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
#endif
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });
    }

    // Circuit breaker policy
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }
}
