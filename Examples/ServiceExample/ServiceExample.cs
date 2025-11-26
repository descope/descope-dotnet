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

            httpClientBuilder
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Register Descope Client using the extension method
            services.AddDescopeClient(
                new DescopeClientOptions
                {
                    ProjectId = config.ProjectId,
                    ManagementKey = config.ManagementKey,
                    HttpClientFactoryName = "DescopeClient",
                    BaseUrl = baseUrl,
                    IsUnsafe = true
                });

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            try
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ServiceExample>>();
                var client = serviceProvider.GetRequiredService<IDescopeClient>();

                logger.LogInformation("Starting user search using DI-based client...");

                // Search for 2 users using V2 API
                var usersResponse = await client.Mgmt.V2.User.Search.PostAsync(
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

                // Demonstrate Auth API with Magic Link flow
                string? testLoginId = null;
                try
                {
                    Console.WriteLine("\n--- Testing Auth API with Magic Link ---");
                    logger.LogInformation("Starting Magic Link authentication flow");

                    // Create a test user using V1 API
                    testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                    logger.LogInformation("Creating test user: {LoginId}", testLoginId);
                    Console.WriteLine($"Creating test user: {testLoginId}");

                    var testUser = await client.Mgmt.V1.User.Create.Test.PostAsync(
                        new Descope.Mgmt.Models.Managementv1.CreateUserRequest
                        {
                            Identifier = testLoginId,
                            Email = testLoginId,
                            VerifiedEmail = true,
                            Name = "Magic Link Test User"
                        });

                    logger.LogInformation("Test user created with ID: {UserId}", testUser?.User?.UserId);
                    Console.WriteLine($"Test user created with ID: {testUser?.User?.UserId}");

                    // Generate magic link for test user with custom claims using V1 API
                    logger.LogInformation("Generating magic link for test user");
                    Console.WriteLine("Generating magic link for test user...");
                    var loginOptions = new Descope.Mgmt.Models.Onetimev1.LoginOptions
                    {
                        CustomClaims = new Descope.Mgmt.Models.Onetimev1.LoginOptions_customClaims
                        {
                            AdditionalData = new Dictionary<string, object>
                            {
                                { "testKey", "testValue" },
                                { "numericKey", 42 }
                            }
                        }
                    };

                    var magicLinkResponse = await client.Mgmt.V1.Tests.Generate.Magiclink.PostAsync(
                        new Descope.Mgmt.Models.Onetimev1.TestUserGenerateMagicLinkRequest
                        {
                            LoginId = testLoginId,
                            DeliveryMethod = "email",
                            RedirectUrl = "https://example.com/auth",
                            LoginOptions = loginOptions
                        });

                    logger.LogInformation("Magic link generated successfully");
                    Console.WriteLine($"Magic link generated: {magicLinkResponse?.Link}");

                    // Extract token from the magic link URL
                    var uri = new Uri(magicLinkResponse!.Link!);
                    var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var token = queryParams["t"];

                    logger.LogDebug("Extracted token from magic link");
                    Console.WriteLine($"Extracted token: {token}");

                    // Verify the magic link token using Auth V1 API
                    logger.LogInformation("Verifying magic link token");
                    Console.WriteLine("Verifying magic link token...");
                    var authResponse = await client.Auth.V1.Magiclink.Verify.PostAsync(
                        new Descope.Auth.Models.Onetimev1.VerifyMagicLinkRequest
                        {
                            Token = token
                        });

                    logger.LogInformation("Magic link verified successfully");
                    Console.WriteLine("Magic link verified successfully!");
                    Console.WriteLine($"  - User ID: {authResponse?.User?.UserId}");
                    Console.WriteLine($"  - Email: {authResponse?.User?.Email}");
                    Console.WriteLine($"  - Session JWT: {authResponse?.SessionJwt?.Substring(0, Math.Min(50, authResponse.SessionJwt.Length))}...");

                    // Validate the session JWT using Auth V1 API - LOCAL VALIDATION (no HTTP call)
                    logger.LogInformation("Validating session JWT locally");
                    Console.WriteLine("Validating session JWT locally (no HTTP call)...");
                    var validatedToken = await client.Auth.ValidateSession(authResponse!.SessionJwt!);
                    logger.LogInformation("Session JWT validated successfully");
                    Console.WriteLine("Session JWT validated successfully!");
                    Console.WriteLine($"  - Subject (User ID): {validatedToken.Subject}");
                    Console.WriteLine($"  - Project ID: {validatedToken.ProjectId}");
                    Console.WriteLine($"  - Expiration: {validatedToken.Expiration}");
                    Console.WriteLine($"  - Claims count: {validatedToken.Claims.Count}");

                    // Check for custom claims in 'nsec'
                    if (validatedToken.Claims.ContainsKey("nsec"))
                    {
                        logger.LogInformation("Custom claims found in token");
                        Console.WriteLine("Custom claims found in 'nsec':");
                        var nsec = validatedToken.Claims["nsec"];
                        Console.WriteLine($"    nsec: {nsec}");
                    }

                    logger.LogInformation("Auth API flow completed successfully");
                    Console.WriteLine("\nAuth API flow completed successfully!");
                }
                catch (DescopeException descopeEx)
                {
                    logger.LogError(descopeEx, "Error in auth flow");
                    Console.WriteLine($"Auth flow error: {descopeEx.Message}");
                    Console.WriteLine($"  - Error Code: {descopeEx.ErrorCode}");
                    Console.WriteLine($"  - Error Description: {descopeEx.ErrorDescription}");
                    Console.WriteLine($"  - Error Message: {descopeEx.ErrorMessage}");
                }
                catch (Exception authEx)
                {
                    logger.LogError(authEx, "Error in auth flow");
                    Console.WriteLine($"Auth flow error (non-Descope): {authEx.Message}");
                }
                finally
                {
                    // Clean up test user using V1 API
                    if (!string.IsNullOrEmpty(testLoginId))
                    {
                        try
                        {
                            logger.LogInformation("Cleaning up test user: {LoginId}", testLoginId);
                            Console.WriteLine($"\nCleaning up test user: {testLoginId}");
                            await client.Mgmt.V1.User.DeletePath.PostAsync(
                                new Descope.Mgmt.Models.Managementv1.DeleteUserRequest
                                {
                                    Identifier = testLoginId
                                });
                            logger.LogInformation("Test user deleted successfully");
                            Console.WriteLine("Test user deleted successfully.");
                            Console.WriteLine("END OF EXAMPLE");
                        }
                        catch (Exception cleanupEx)
                        {
                            logger.LogWarning(cleanupEx, "Error cleaning up test user");
                            Console.WriteLine($"Cleanup error: {cleanupEx.Message}");
                            Console.WriteLine("END OF EXAMPLE");
                        }
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
