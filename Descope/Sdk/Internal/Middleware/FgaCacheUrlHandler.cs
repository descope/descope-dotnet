namespace Descope.Internal;

/// <summary>
/// Routes specific FGA cache operations to an alternate base URL when FgaCacheUrl is configured.
/// Affected endpoints (POST only):
/// - /v1/mgmt/fga/schema (SaveSchema)
/// - /v1/mgmt/fga/relations (CreateRelations)
/// - /v1/mgmt/fga/relations/delete (DeleteRelations)
/// - /v1/mgmt/fga/check (Check)
/// </summary>
internal class FgaCacheUrlHandler : DelegatingHandler
{
    private readonly string? _fgaCacheUrl;

    // Endpoints that should use the FGA cache URL (POST requests only)
    private static readonly string[] CacheEndpoints = new[]
    {
        "/v1/mgmt/fga/schema",          // SaveSchema (POST only, GET uses BaseUrl)
        "/v1/mgmt/fga/relations",       // CreateRelations (POST only)
        "/v1/mgmt/fga/relations/delete", // DeleteRelations (POST)
        "/v1/mgmt/fga/check"            // Check (POST)
    };

    public FgaCacheUrlHandler(string? fgaCacheUrl)
    {
        // Normalize by removing trailing slashes
        _fgaCacheUrl = string.IsNullOrWhiteSpace(fgaCacheUrl)
            ? fgaCacheUrl
            : fgaCacheUrl!.TrimEnd('/');
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Only route to cache URL if:
        // 1. FgaCacheUrl is configured
        // 2. Request is a POST (GET requests like LoadSchema should use BaseUrl)
        // 3. Request path matches one of the cache endpoints exactly
        if (!string.IsNullOrWhiteSpace(_fgaCacheUrl) &&
            request.Method == HttpMethod.Post &&
            request.RequestUri != null)
        {
            var path = request.RequestUri.AbsolutePath;

            foreach (var endpoint in CacheEndpoints)
            {
                // Check for exact match (path equals endpoint or path equals endpoint + trailing slash)
                if (path == endpoint || path == endpoint + "/")
                {
                    // Replace the base URL portion with the cache URL
                    var newUri = new Uri(_fgaCacheUrl + path + request.RequestUri.Query);
                    request.RequestUri = newUri;
                    break;
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
