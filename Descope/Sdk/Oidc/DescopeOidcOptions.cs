#if NET6_0_OR_GREATER
namespace Descope;

/// <summary>
/// Configuration options for Descope OIDC authentication.
/// Used with <see cref="AuthenticationBuilderExtensions.AddDescopeOidc"/>.
/// </summary>
/// <remarks>
/// This class provides a simplified configuration experience for integrating Descope
/// as an OpenID Connect provider in ASP.NET Core applications.
///
/// <example>
/// <code>
/// builder.Services.AddAuthentication()
///     .AddDescopeOidc(options =>
///     {
///         options.ProjectId = "your_project_id";
///     });
/// </code>
/// </example>
/// </remarks>
public class DescopeOidcOptions
{
    /// <summary>
    /// The Descope Project ID (required).
    /// This is used as the OIDC Client ID.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret for the OIDC flow.
    /// Required for Confidential Client flows. Not required when using PKCE (Proof Key for Code Exchange).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The base URL for the Descope API.
    /// If not set, will be determined based on the project ID's region.
    /// Default: https://api.descope.com (or regional URL if project ID is 32+ characters)
    /// </summary>
    public string? BaseUrl
    {
        get => string.IsNullOrEmpty(_baseUrl) ? DescopeClientOptions.GetBaseUrlForProjectId(ProjectId) : _baseUrl;
        set => _baseUrl = value;
    }
    private string? _baseUrl;

    /// <summary>
    /// The callback path where Descope will redirect after authentication.
    /// Default: "/signin-descope"
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-descope";

    /// <summary>
    /// The path to redirect to after signing out.
    /// Default: "/signout-callback-descope"
    /// </summary>
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-descope";

    /// <summary>
    /// The redirect URI after logout.
    /// If not set, uses the application's base URL.
    /// </summary>
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>
    /// The OIDC scopes to request.
    /// Default: "openid profile email"
    /// </summary>
    public string Scope { get; set; } = "openid profile email";

    /// <summary>
    /// Whether to use PKCE (Proof Key for Code Exchange) for the authorization code flow.
    /// Default: true (recommended for security)
    /// </summary>
    public bool UsePkce { get; set; } = true;

    /// <summary>
    /// Whether to save tokens in the authentication properties.
    /// Default: true
    /// </summary>
    public bool SaveTokens { get; set; } = true;

    /// <summary>
    /// Whether to get claims from the UserInfo endpoint after token validation.
    /// Default: false
    /// </summary>
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = false;

    /// <summary>
    /// The authentication scheme name.
    /// Default: "Descope"
    /// </summary>
    public string AuthenticationScheme { get; set; } = "Descope";

    /// <summary>
    /// Whether to require HTTPS for the metadata endpoint.
    /// Set to false for development with local HTTP servers.
    /// Default: true (recommended for production)
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// The Descope Flow ID to use for authentication.
    /// This is appended as a "flow" query parameter to the authorization URL.
    /// Example: "sign-up-or-in", "custom-flow-id"
    /// </summary>
    public string? FlowId { get; set; }

    /// <summary>
    /// Gets the OIDC Authority (issuer) URL for this configuration.
    /// </summary>
    /// <returns>The authority URL in the format "{BaseUrl}/{ProjectId}"</returns>
    public string GetAuthority()
    {
        return $"{BaseUrl!.TrimEnd('/')}/{ProjectId}";
    }

    /// <summary>
    /// Validates that required options are set.
    /// </summary>
    /// <exception cref="DescopeException">Thrown when ProjectId is not set.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectId))
        {
            throw new DescopeException("ProjectId is required for Descope OIDC authentication");
        }
    }
}
#endif
