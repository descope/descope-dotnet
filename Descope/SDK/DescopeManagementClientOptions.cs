namespace Descope;

/// <summary>
/// Configuration options for the Descope Management Client.
/// </summary>
public class DescopeManagementClientOptions
{
    /// <summary>
    /// The Descope Project ID (required).
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// The Descope Management Key (required).
    /// </summary>
    public string ManagementKey { get; set; } = string.Empty;

    /// <summary>
    /// The base URL for the Descope API.
    /// Default: https://api.descope.com
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.descope.com";

    /// <summary>
    /// The name of the HttpClient to use from IHttpClientFactory.
    /// Only used when registering via dependency injection.
    /// </summary>
    public string? HttpClientFactoryName { get; set; }

    /// <summary>
    /// If true, allows unsafe HTTPS calls by accepting any server certificate.
    /// Only used for instance-based clients (non-DI).
    /// Default: false
    /// WARNING: Only use this for development/testing purposes.
    /// </summary>
    public bool IsUnsafe { get; set; } = false;

    /// <summary>
    /// Validates that required options are set.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectId))
        {
            throw new ArgumentException("ProjectId is required", nameof(ProjectId));
        }

        if (string.IsNullOrWhiteSpace(ManagementKey))
        {
            throw new ArgumentException("ManagementKey is required", nameof(ManagementKey));
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new ArgumentException("BaseUrl cannot be empty", nameof(BaseUrl));
        }
    }
}
