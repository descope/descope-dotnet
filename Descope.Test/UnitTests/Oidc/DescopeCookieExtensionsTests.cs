#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Descope.Test.UnitTests.Oidc;

public class DescopeCookieExtensionsTests
{
    private static AuthenticationBuilder CreateAuthenticationBuilder()
    {
        var services = new ServiceCollection();
        return services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    #region AddDescopeCookies Basic Configuration

    [Fact]
    public async Task AddDescopeCookies_RegistersCookieAuthenticationScheme()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var schemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.NotNull(scheme);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, scheme.Name);
    }

    [Fact]
    public void AddDescopeCookies_ConfiguresSameSiteLax()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(SameSiteMode.Lax, cookieOptions.Cookie.SameSite);
    }

    [Fact]
    public void AddDescopeCookies_ConfiguresSecureAlways()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(CookieSecurePolicy.Always, cookieOptions.Cookie.SecurePolicy);
    }

    [Fact]
    public void AddDescopeCookies_ConfiguresHttpOnlyTrue()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.True(cookieOptions.Cookie.HttpOnly);
    }

    [Fact]
    public void AddDescopeCookies_ConfiguresOneHourExpiration()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(TimeSpan.FromHours(1), cookieOptions.ExpireTimeSpan);
    }

    [Fact]
    public void AddDescopeCookies_EnablesSlidingExpiration()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.True(cookieOptions.SlidingExpiration);
    }

    #endregion

    #region AddDescopeCookies Custom Configuration

    [Fact]
    public void AddDescopeCookies_AllowsCustomConfiguration()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies(options =>
        {
            options.LoginPath = "/custom-login";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal("/custom-login", cookieOptions.LoginPath);
        Assert.Equal(TimeSpan.FromHours(8), cookieOptions.ExpireTimeSpan);
    }

    [Fact]
    public void AddDescopeCookies_CustomConfigurationOverridesDefaults()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.SlidingExpiration = false;
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(SameSiteMode.Strict, cookieOptions.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.SameAsRequest, cookieOptions.Cookie.SecurePolicy);
        Assert.False(cookieOptions.SlidingExpiration);
    }

    [Fact]
    public void AddDescopeCookies_AllowsCustomCookieName()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies(options =>
        {
            options.Cookie.Name = "MyCustomCookie";
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal("MyCustomCookie", cookieOptions.Cookie.Name);
    }

    [Fact]
    public void AddDescopeCookies_WithNullConfiguration_UsesDefaults()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeCookies(null);
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(SameSiteMode.Lax, cookieOptions.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, cookieOptions.Cookie.SecurePolicy);
        Assert.True(cookieOptions.Cookie.HttpOnly);
    }

    #endregion

    #region AddDescopeCookies Chaining

    [Fact]
    public void AddDescopeCookies_ReturnsAuthenticationBuilder_ForChaining()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        var result = builder.AddDescopeCookies();

        // Assert
        Assert.Same(builder, result);
    }

    #endregion

    #region AddDescopeOidcAuthentication Combined Setup

    [Fact]
    public async Task AddDescopeOidcAuthentication_RegistersBothCookieAndOidcSchemes()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidcAuthentication(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var schemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();

        var cookieScheme = await schemeProvider.GetSchemeAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.NotNull(cookieScheme);

        var oidcScheme = await schemeProvider.GetSchemeAsync("Descope");
        Assert.NotNull(oidcScheme);
    }

    [Fact]
    public void AddDescopeOidcAuthentication_ConfiguresCookieDefaults()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidcAuthentication(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(SameSiteMode.Lax, cookieOptions.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, cookieOptions.Cookie.SecurePolicy);
        Assert.True(cookieOptions.Cookie.HttpOnly);
        Assert.Equal(TimeSpan.FromHours(1), cookieOptions.ExpireTimeSpan);
        Assert.True(cookieOptions.SlidingExpiration);
    }

    [Fact]
    public void AddDescopeOidcAuthentication_ConfiguresOidcOptions()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidcAuthentication(options =>
        {
            options.ProjectId = "P123456789";
            options.ClientSecret = "my-secret";
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("https://api.descope.com/P123456789", oidcOptions.Authority);
        Assert.Equal("P123456789", oidcOptions.ClientId);
        Assert.Equal("my-secret", oidcOptions.ClientSecret);
    }

    [Fact]
    public void AddDescopeOidcAuthentication_AllowsCustomCookieConfiguration()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidcAuthentication(
            configureOidc: options => options.ProjectId = "P123456789",
            configureCookies: options =>
            {
                options.Cookie.Name = "CustomCookie";
                options.ExpireTimeSpan = TimeSpan.FromHours(4);
            });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal("CustomCookie", cookieOptions.Cookie.Name);
        Assert.Equal(TimeSpan.FromHours(4), cookieOptions.ExpireTimeSpan);
    }

    [Fact]
    public void AddDescopeOidcAuthentication_AllowsCustomEventConfiguration()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidcAuthentication(
            configureOidc: options => options.ProjectId = "P123456789",
            configureEvents: events =>
            {
                events.OnTokenResponseReceived += _ => Task.CompletedTask;
            });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert - verify OIDC options are configured with events
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");
        Assert.NotNull(oidcOptions.Events);
    }

    [Fact]
    public void AddDescopeOidcAuthentication_WithEmptyProjectId_ThrowsDescopeException()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act & Assert
        var exception = Assert.Throws<DescopeException>(() =>
            builder.AddDescopeOidcAuthentication(options => options.ProjectId = ""));

        Assert.Contains("ProjectId is required", exception.Message);
    }

    [Fact]
    public void AddDescopeOidcAuthentication_WithNullCookieConfiguration_UsesDefaults()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidcAuthentication(
            configureOidc: options => options.ProjectId = "P123456789",
            configureCookies: null,
            configureEvents: null);
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal(SameSiteMode.Lax, cookieOptions.Cookie.SameSite);
    }

    #endregion

    #region AddDescopeOidcAuthentication Chaining

    [Fact]
    public void AddDescopeOidcAuthentication_ReturnsAuthenticationBuilder_ForChaining()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        var result = builder.AddDescopeOidcAuthentication(options => options.ProjectId = "P123456789");

        // Assert
        Assert.Same(builder, result);
    }

    #endregion

    #region Regional URL Support in Combined Setup

    [Fact]
    public void AddDescopeOidcAuthentication_UsesRegionalUrl_ForLongProjectId()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();
        // 32+ character project ID with region at positions 1-4
        const string projectId = "Puse1abcdefghijklmnopqrstuvwxyz12";

        // Act
        builder.AddDescopeOidcAuthentication(options => options.ProjectId = projectId);
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal($"https://api.use1.descope.com/{projectId}", oidcOptions.Authority);
    }

    #endregion
}
#endif
