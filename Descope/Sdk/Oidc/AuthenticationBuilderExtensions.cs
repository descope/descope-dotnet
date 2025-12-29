#if NET6_0_OR_GREATER
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Descope;

/// <summary>
/// Extension methods for adding Descope OIDC authentication to an <see cref="AuthenticationBuilder"/>.
/// </summary>
public static class AuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds Descope OpenID Connect authentication to the authentication builder.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="configureOptions">Action to configure the <see cref="DescopeOidcOptions"/>.</param>
    /// <returns>The authentication builder for chaining.</returns>
    /// <remarks>
    /// This method configures OpenID Connect authentication using Descope as the identity provider.
    /// It uses the Authorization Code flow with PKCE by default for security.
    ///
    /// <example>
    /// <code>
    /// builder.Services.AddAuthentication(options =>
    /// {
    ///     options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    ///     options.DefaultChallengeScheme = "Descope";
    /// })
    /// .AddCookie()
    /// .AddDescopeOidc(options =>
    /// {
    ///     options.ProjectId = builder.Configuration["Descope:ProjectId"]!;
    ///     options.ClientSecret = builder.Configuration["Descope:ClientSecret"]; // Optional for PKCE
    ///     options.FlowId = "sign-up-or-in"; // Optional: specify Descope flow
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public static AuthenticationBuilder AddDescopeOidc(
        this AuthenticationBuilder builder,
        Action<DescopeOidcOptions> configureOptions)
    {
        return builder.AddDescopeOidc(configureOptions, null);
    }

    public static AuthenticationBuilder AddDescopeOidc(
        this AuthenticationBuilder builder,
        Action<DescopeOidcOptions> configureOptions,
        Action<OpenIdConnectEvents>? configureEvents)
    {
        var descopeOptions = new DescopeOidcOptions();
        configureOptions(descopeOptions);
        descopeOptions.Validate();

        return builder.AddOpenIdConnect(descopeOptions.AuthenticationScheme, descopeOptions.AuthenticationScheme, oidcOptions =>
        {
            // Set the authority (issuer) URL
            oidcOptions.Authority = descopeOptions.GetAuthority();

            // Client ID is the Descope Project ID
            oidcOptions.ClientId = descopeOptions.ProjectId;

            // Client secret (optional for PKCE flows)
            if (!string.IsNullOrEmpty(descopeOptions.ClientSecret))
            {
                oidcOptions.ClientSecret = descopeOptions.ClientSecret;
            }

            // Response type: authorization code flow + PKCE
            oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
            oidcOptions.UsePkce = descopeOptions.UsePkce;

            // Configure scopes
            oidcOptions.Scope.Clear();
            foreach (var scope in descopeOptions.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                oidcOptions.Scope.Add(scope);
            }

            // Callback paths
            oidcOptions.CallbackPath = descopeOptions.CallbackPath;
            oidcOptions.SignedOutCallbackPath = descopeOptions.SignedOutCallbackPath;

            if (!string.IsNullOrEmpty(descopeOptions.PostLogoutRedirectUri))
            {
                oidcOptions.SignedOutRedirectUri = descopeOptions.PostLogoutRedirectUri;
            }

            // Save tokens for later use
            oidcOptions.SaveTokens = descopeOptions.SaveTokens;

            // Get claims from userinfo endpoint
            oidcOptions.GetClaimsFromUserInfoEndpoint = descopeOptions.GetClaimsFromUserInfoEndpoint;

            // HTTPS metadata requirement (set to false for local development)
            oidcOptions.RequireHttpsMetadata = descopeOptions.RequireHttpsMetadata;

            // Configure correlation and nonce cookies for HTTP development scenarios.
            // When running on HTTP (not HTTPS), browsers require SameSite=Lax or Strict
            // because SameSite=None requires the Secure flag which only works on HTTPS.
            // This is especially important for .NET 6.0 where the default SameSite=None
            // causes "Correlation failed" errors when running on HTTP.
            if (!descopeOptions.RequireHttpsMetadata)
            {
                oidcOptions.CorrelationCookie.SameSite = SameSiteMode.Lax;
                oidcOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                oidcOptions.NonceCookie.SameSite = SameSiteMode.Lax;
                oidcOptions.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            }

            // Map Descope claims to standard claim types
            oidcOptions.TokenValidationParameters.NameClaimType = "name";
            oidcOptions.TokenValidationParameters.RoleClaimType = "roles";

            // Add Descope-specific event handlers using += to preserve built-in behavior
            if (!string.IsNullOrWhiteSpace(descopeOptions.FlowId))
            {
                oidcOptions.Events.OnRedirectToIdentityProvider += context =>
                {
                    context.ProtocolMessage.SetParameter("flow", descopeOptions.FlowId);
                    return Task.CompletedTask;
                };
            }

            // Allow user customization of events
            configureEvents?.Invoke(oidcOptions.Events);
        });
    }

    /// <summary>
    /// Adds Descope OpenID Connect authentication using a configuration section.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="options">The pre-configured <see cref="DescopeOidcOptions"/>.</param>
    /// <returns>The authentication builder for chaining.</returns>
    public static AuthenticationBuilder AddDescopeOidc(
        this AuthenticationBuilder builder,
        DescopeOidcOptions options)
    {
        return builder.AddDescopeOidc(o =>
        {
            o.ProjectId = options.ProjectId;
            o.ClientSecret = options.ClientSecret;
            o.BaseUrl = options.BaseUrl;
            o.CallbackPath = options.CallbackPath;
            o.SignedOutCallbackPath = options.SignedOutCallbackPath;
            o.PostLogoutRedirectUri = options.PostLogoutRedirectUri;
            o.Scope = options.Scope;
            o.UsePkce = options.UsePkce;
            o.SaveTokens = options.SaveTokens;
            o.GetClaimsFromUserInfoEndpoint = options.GetClaimsFromUserInfoEndpoint;
            o.AuthenticationScheme = options.AuthenticationScheme;
            o.RequireHttpsMetadata = options.RequireHttpsMetadata;
            o.FlowId = options.FlowId;
        });
    }
}
#endif
