using Descope;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Text;
using System.Text.Json;

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
/// Custom HTTP message handler that logs request and response details.
/// </summary>
public class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<HttpLoggingHandler> _logger;

    public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Log request details
        _logger.LogInformation("=== HTTP Request ===");
        _logger.LogInformation("Method: {Method}", request.Method);
        _logger.LogInformation("URI: {Uri}", request.RequestUri);

        // Log request headers
        _logger.LogInformation("--- Request Headers ---");
        foreach (var header in request.Headers)
        {
            _logger.LogInformation("  {HeaderName}: {HeaderValue}",
                header.Key,
                string.Join(", ", header.Value));
        }

        // Log request body if present
        if (request.Content != null)
        {
            // Log content headers
            _logger.LogInformation("--- Request Content Headers ---");
            foreach (var header in request.Content.Headers)
            {
                _logger.LogInformation("  {HeaderName}: {HeaderValue}",
                    header.Key,
                    string.Join(", ", header.Value));
            }

            // Log request body
            var requestBody = await request.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(requestBody))
            {
                _logger.LogInformation("--- Request Body ---");
                // Try to format JSON for better readability
                try
                {
                    var jsonDoc = JsonDocument.Parse(requestBody);
                    var formattedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    _logger.LogInformation("{RequestBody}", formattedJson);
                }
                catch
                {
                    // If not JSON, log as-is
                    _logger.LogInformation("{RequestBody}", requestBody);
                }

                // Recreate the content since ReadAsStringAsync consumes the stream
                request.Content = new StringContent(requestBody, Encoding.UTF8,
                    request.Content.Headers.ContentType?.MediaType ?? "application/json");
            }
        }

        _logger.LogInformation("==================");

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Log response status
        _logger.LogInformation("=== HTTP Response ===");
        _logger.LogInformation("Status Code: {StatusCode} ({StatusCodeNumber})",
            response.StatusCode,
            (int)response.StatusCode);

        // Log response headers
        _logger.LogInformation("--- Response Headers ---");
        foreach (var header in response.Headers)
        {
            _logger.LogInformation("  {HeaderName}: {HeaderValue}",
                header.Key,
                string.Join(", ", header.Value));
        }

        if (response.Content != null)
        {
            _logger.LogInformation("--- Response Content Headers ---");
            foreach (var header in response.Content.Headers)
            {
                _logger.LogInformation("  {HeaderName}: {HeaderValue}",
                    header.Key,
                    string.Join(", ", header.Value));
            }
        }

        _logger.LogInformation("====================");

        return response;
    }
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

            // Add our custom logging handler FIRST in the pipeline
            httpClientBuilder.AddHttpMessageHandler(sp =>
                new HttpLoggingHandler(sp.GetRequiredService<ILogger<HttpLoggingHandler>>()));

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

                    // update user email using Auth V1 API
                    Console.WriteLine("Updating user email using Auth V1 API...");
                    var newEmail = "updated_" + testLoginId;
                    // Using the extension method with explicit JWT parameter
                    var updateEmailResponse = await client.Auth.V1.Magiclink.Update.Email.PostWithJwtAsync(
                        new Descope.Auth.Models.Onetimev1.UpdateUserEmailMagicLinkRequest
                        {
                            LoginId = testLoginId,
                            Email = newEmail,
                            RedirectUrl = "https://example.com/email-updated",
                            AddToLoginIDs = true,
                            OnMergeUseExisting = true,
                        },
                        authResponse!.RefreshJwt!);
                    Console.WriteLine($"User email update magic link generated: {updateEmailResponse?.MaskedEmail}");

                    // Validate the session JWT using Auth V1 API - LOCAL VALIDATION (no HTTP call)
                    logger.LogInformation("Validating session JWT locally");
                    Console.WriteLine("Validating session JWT locally (no HTTP call)...");
                    var validatedToken = await client.Auth.ValidateSessionAsync(authResponse!.SessionJwt!);
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
