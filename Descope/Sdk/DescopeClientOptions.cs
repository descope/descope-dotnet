namespace Descope;

/// <summary>
/// Configuration options for the Descope Client.
/// </summary>
public class DescopeClientOptions
{
    /// <summary>
    /// The Descope Project ID (required).
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// The Descope Management Key (optional).
    /// Required for accessing management APIs.
    /// </summary>
    public string? ManagementKey { get; set; }

    /// <summary>
    /// The Descope Auth Management Key (optional).
    /// Used to provide a management key to use with Authentication APIs whose public access has been disabled.
    /// If not set, only enabled auth APIs can be accessed.
    /// </summary>
    public string? AuthManagementKey { get; set; }

    /// <summary>
    /// The base URL for the Descope API.
    /// If not set, will be determined based on the project ID's region.
    /// Default: https://api.descope.com (or regional URL if project ID is 32+ characters)
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// The FGA cache URL for specific FGA operations (SaveSchema, CreateRelations, DeleteRelations, Check).
    /// If not set, FGA cache operations will use the standard BaseUrl.
    /// Must be a valid HTTP or HTTPS URL if provided.
    /// </summary>
    public string? FgaCacheUrl { get; set; }

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
    /// How long the public signing keys (JWKS) fetched from <c>/v2/keys/{projectId}</c>
    /// are cached before the next session validation triggers a refresh.
    /// Increasing this reduces the number of key-fetch requests for high-traffic services.
    /// Default: 5 minutes.
    /// </summary>
    /// <remarks>
    /// Raising this value is safe even during key rotation: session validation performs an
    /// immediate key re-fetch (bypassing this TTL) whenever a token is signed with a key ID
    /// that is not already cached, so tokens from newly rotated keys are still accepted right away.
    /// Must be greater than zero.
    /// </remarks>
    public TimeSpan JwksCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Validates that required options are set.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectId))
        {
            throw new DescopeException("ProjectId is required");
        }

        if (JwksCacheDuration <= TimeSpan.Zero)
        {
            throw new DescopeException("JwksCacheDuration must be greater than zero");
        }

        if (!string.IsNullOrWhiteSpace(FgaCacheUrl))
        {
            var normalizedUrl = FgaCacheUrl!.TrimEnd('/');
            if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new DescopeException("FgaCacheUrl must be a valid HTTP or HTTPS URL");
            }
        }
    }

    /// <summary>
    /// Determines the appropriate base URL for the given project ID.
    /// If the project ID is 32 characters or longer, extracts the region from positions 1-4
    /// and constructs a regional URL. Otherwise, returns the default URL.
    /// </summary>
    /// <param name="projectId">The Descope project ID.</param>
    /// <returns>The base URL for the project.</returns>
    internal static string GetBaseUrlForProjectId(string projectId)
    {
        const string defaultApiPrefix = "https://api";
        const string defaultDomainName = "descope.com";
        const string defaultUrl = defaultApiPrefix + "." + defaultDomainName;

        if (string.IsNullOrEmpty(projectId) || projectId.Length < 32)
        {
            return defaultUrl;
        }

        // Extract region from positions 1-4 (0-indexed, so substring from index 1, length 4)
        string region = projectId.Substring(1, 4);
        return $"{defaultApiPrefix}.{region}.{defaultDomainName}";
    }
}
