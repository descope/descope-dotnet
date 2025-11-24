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

            // Search for 2 users
            var usersResponse = await client.V2.Mgmt.User.Search.PostAsync(
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
