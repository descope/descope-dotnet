#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Descope;

/// <summary>
/// Extension methods for adding Descope cookie authentication support.
/// </summary>
public static class DescopeCookieExtensions
{
    /// <summary>
    /// Adds cookie authentication configured for use with Descope OIDC.
    /// This is a convenience method that sets up cookie authentication with sensible defaults
    /// for use alongside <see cref="AuthenticationBuilderExtensions.AddDescopeOidc"/>.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="configureCookies">Optional action to customize cookie options.</param>
    /// <returns>The authentication builder for chaining.</returns>
    /// <remarks>
    /// This method sets up cookie authentication with the following defaults:
    /// - SameSite: Lax (required for OAuth redirects)
    /// - Secure: Always (for production security)
    /// - HttpOnly: true (prevents XSS attacks)
    ///
    /// <example>
    /// <code>
    /// // Simple setup
    /// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    ///     .AddDescopeCookies()
    ///     .AddDescopeOidc(options => options.ProjectId = "your_project_id");
    ///
    /// // With customization
    /// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    ///     .AddDescopeCookies(options =>
    ///     {
    ///         options.LoginPath = "/login";
    ///         options.ExpireTimeSpan = TimeSpan.FromHours(8);
    ///     })
    ///     .AddDescopeOidc(options => options.ProjectId = "your_project_id");
    /// </code>
    /// </example>
    /// </remarks>
    public static AuthenticationBuilder AddDescopeCookies(
        this AuthenticationBuilder builder,
        Action<CookieAuthenticationOptions>? configureCookies = null)
    {
        return builder.AddCookie(options =>
        {
            // Set sensible and safe defaults for OIDC flows
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;

            // Reasonable session duration
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
            options.SlidingExpiration = true;

            // Allow user customization
            configureCookies?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds Descope authentication with both cookies and OIDC in one call.
    /// This is the simplest way to add Descope authentication to an application.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="configureOidc">Action to configure the <see cref="DescopeOidcOptions"/>.</param>
    /// <param name="configureCookies">Optional action to customize cookie options.</param>
    /// <returns>The authentication builder for chaining.</returns>
    /// <remarks>
    /// This method combines <see cref="AddDescopeCookies"/> and
    /// <see cref="AuthenticationBuilderExtensions.AddDescopeOidc"/> for a simplified setup.
    ///
    /// <example>
    /// <code>
    /// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    ///     .AddDescopeWebAppAuthentication(options =>
    ///     {
    ///         options.ProjectId = "your_project_id";
    ///         options.ClientSecret = "optional_for_pkce";
    ///         options.FlowId = "sign-up-or-in"; // Optional: specify Descope flow
    ///     });
    /// </code>
    /// </example>
    /// </remarks>
    public static AuthenticationBuilder AddDescopeOidcAuthentication(
        this AuthenticationBuilder builder,
        Action<DescopeOidcOptions> configureOidc,
        Action<CookieAuthenticationOptions>? configureCookies = null,
        Action<OpenIdConnectEvents>? configureEvents = null)
    {
        return builder
            .AddDescopeCookies(configureCookies)
            .AddDescopeOidc(configureOidc, configureEvents);
    }
}
#endif
