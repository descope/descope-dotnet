using Descope;
using Descope.Mgmt.Models;
using System;
using System.IO;
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
/// Example demonstrating how to use the Descope Management Client as a direct instance.
/// This example shows a simpler approach without dependency injection, suitable for
/// simple applications or scripts.
/// </summary>
public class InstanceExample
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

            Console.WriteLine("Creating Descope Management Client instance...");

            // Create a direct instance of the client using the factory
            var client = DescopeManagementClientFactory.Create(
                projectId: config.ProjectId,
                managementKey: config.ManagementKey,
                baseUrl: baseUrl,
                isUnsafe: config.Unsafe);

            Console.WriteLine("Starting user search using instance-based client...");

            // Search for 2 users using V2 API
            var usersResponse = await client.Mgmt.V2.User.Search.PostAsync(
                new Descope.Mgmt.Models.Managementv1.SearchUsersRequest
                {
                    Limit = 2
                });

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

                // Create a test user using V1 API
                testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                Console.WriteLine($"Creating test user: {testLoginId}");

                var testUser = await client.Mgmt.V1.User.Create.Test.PostAsync(
                    new Descope.Mgmt.Models.Managementv1.CreateUserRequest
                    {
                        Identifier = testLoginId,
                        Email = testLoginId,
                        VerifiedEmail = true,
                        Name = "Magic Link Test User"
                    });

                Console.WriteLine($"Test user created with ID: {testUser?.User?.UserId}");

                // Generate magic link for test user with custom claims using V1 API
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

                Console.WriteLine($"Magic link generated: {magicLinkResponse?.Link}");

                // Extract token from the magic link URL
                var uri = new Uri(magicLinkResponse!.Link!);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var token = queryParams["t"];

                Console.WriteLine($"Extracted token: {token}");

                // Verify the magic link token using Auth V1 API
                Console.WriteLine("Verifying magic link token...");
                var authResponse = await client.Auth.V1.Magiclink.Verify.PostAsync(
                    new Descope.Auth.Models.Onetimev1.VerifyMagicLinkRequest
                    {
                        Token = token
                    });

                Console.WriteLine("Magic link verified successfully!");
                Console.WriteLine($"  - User ID: {authResponse?.User?.UserId}");
                Console.WriteLine($"  - Email: {authResponse?.User?.Email}");
                Console.WriteLine($"  - Session JWT: {authResponse?.SessionJwt?.Substring(0, Math.Min(50, authResponse.SessionJwt.Length))}...");

                // Validate the session JWT using Auth V1 API - LOCAL VALIDATION (no HTTP call)
                Console.WriteLine("Validating session JWT locally (no HTTP call)...");
                var validatedToken = await client.Auth.ValidateSession(authResponse?.SessionJwt ?? string.Empty);

                Console.WriteLine("Session JWT validated successfully!");
                Console.WriteLine($"  - Subject (User ID): {validatedToken.Subject}");
                Console.WriteLine($"  - Project ID: {validatedToken.ProjectId}");
                Console.WriteLine($"  - Expiration: {validatedToken.Expiration}");
                Console.WriteLine($"  - Claims count: {validatedToken.Claims.Count}");

                // Check for custom claims in 'nsec'
                if (validatedToken.Claims.ContainsKey("nsec"))
                {
                    Console.WriteLine("Custom claims found in 'nsec':");
                    var nsec = validatedToken.Claims["nsec"];
                    Console.WriteLine($"    nsec: {nsec}");
                }

                Console.WriteLine("\nAuth API flow completed successfully!");
            }
            catch (Exception authEx)
            {
                Console.WriteLine($"Auth flow error: {authEx.Message}");
            }
            finally
            {
                // Clean up test user using V1 API
                if (!string.IsNullOrEmpty(testLoginId))
                {
                    try
                    {
                        Console.WriteLine($"\nCleaning up test user: {testLoginId}");
                        await client.Mgmt.V1.User.DeletePath.PostAsync(
                            new Descope.Mgmt.Models.Managementv1.DeleteUserRequest
                            {
                                Identifier = testLoginId
                            });
                        Console.WriteLine("Test user deleted successfully.");
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"Cleanup error: {cleanupEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("END OF EXAMPLE");
    }
}
