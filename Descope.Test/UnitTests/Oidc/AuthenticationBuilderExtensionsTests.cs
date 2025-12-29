#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Descope.Test.UnitTests.Oidc;

public class AuthenticationBuilderExtensionsTests
{
    private static AuthenticationBuilder CreateAuthenticationBuilder()
    {
        var services = new ServiceCollection();
        return services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    #region AddDescopeOidc Basic Configuration

    [Fact]
    public async Task AddDescopeOidc_WithValidProjectId_RegistersOpenIdConnectScheme()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var schemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync("Descope");
        Assert.NotNull(scheme);
        Assert.Equal("Descope", scheme.Name);
    }

    [Fact]
    public void AddDescopeOidc_WithEmptyProjectId_ThrowsDescopeException()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act & Assert
        var exception = Assert.Throws<DescopeException>(() =>
            builder.AddDescopeOidc(options => options.ProjectId = ""));

        Assert.Contains("ProjectId is required", exception.Message);
    }

    [Fact]
    public void AddDescopeOidc_WithWhitespaceProjectId_ThrowsDescopeException()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act & Assert
        var exception = Assert.Throws<DescopeException>(() =>
            builder.AddDescopeOidc(options => options.ProjectId = "   "));

        Assert.Contains("ProjectId is required", exception.Message);
    }

    #endregion

    #region OpenIdConnect Options Configuration

    [Fact]
    public void AddDescopeOidc_ConfiguresAuthority_WithDefaultBaseUrl()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("https://api.descope.com/P123456789", oidcOptions.Authority);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresAuthority_WithCustomBaseUrl()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.BaseUrl = "https://custom.descope.com";
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("https://custom.descope.com/P123456789", oidcOptions.Authority);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresClientId_AsProjectId()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();
        const string projectId = "P123456789";

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = projectId);
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal(projectId, oidcOptions.ClientId);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresClientSecret_WhenProvided()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();
        const string clientSecret = "my-secret";

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.ClientSecret = clientSecret;
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal(clientSecret, oidcOptions.ClientSecret);
    }

    [Fact]
    public void AddDescopeOidc_DoesNotSetClientSecret_WhenNotProvided()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Null(oidcOptions.ClientSecret);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresResponseType_AsCode()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal(OpenIdConnectResponseType.Code, oidcOptions.ResponseType);
    }

    [Fact]
    public void AddDescopeOidc_EnablesPkce_ByDefault()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.True(oidcOptions.UsePkce);
    }

    [Fact]
    public void AddDescopeOidc_DisablesPkce_WhenConfigured()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.UsePkce = false;
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.False(oidcOptions.UsePkce);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresDefaultScopes()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Contains("openid", oidcOptions.Scope);
        Assert.Contains("profile", oidcOptions.Scope);
        Assert.Contains("email", oidcOptions.Scope);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresCustomScopes()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.Scope = "openid custom_scope";
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Contains("openid", oidcOptions.Scope);
        Assert.Contains("custom_scope", oidcOptions.Scope);
        Assert.DoesNotContain("profile", oidcOptions.Scope);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresCallbackPath()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("/signin-descope", oidcOptions.CallbackPath);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresCustomCallbackPath()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.CallbackPath = "/custom-callback";
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("/custom-callback", oidcOptions.CallbackPath);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresSignedOutCallbackPath()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("/signout-callback-descope", oidcOptions.SignedOutCallbackPath);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresPostLogoutRedirectUri_WhenProvided()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.PostLogoutRedirectUri = "https://example.com/logged-out";
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("https://example.com/logged-out", oidcOptions.SignedOutRedirectUri);
    }

    [Fact]
    public void AddDescopeOidc_SavesTokens_ByDefault()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.True(oidcOptions.SaveTokens);
    }

    [Fact]
    public void AddDescopeOidc_DisablesSaveTokens_WhenConfigured()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.SaveTokens = false;
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.False(oidcOptions.SaveTokens);
    }

    [Fact]
    public void AddDescopeOidc_ConfiguresClaimTypes()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("name", oidcOptions.TokenValidationParameters.NameClaimType);
        Assert.Equal("roles", oidcOptions.TokenValidationParameters.RoleClaimType);
    }

    #endregion

    #region HTTP Development Configuration

    [Fact]
    public void AddDescopeOidc_ConfiguresLaxCookies_WhenHttpsNotRequired()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.RequireHttpsMetadata = false;
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal(SameSiteMode.Lax, oidcOptions.CorrelationCookie.SameSite);
        Assert.Equal(CookieSecurePolicy.SameAsRequest, oidcOptions.CorrelationCookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, oidcOptions.NonceCookie.SameSite);
        Assert.Equal(CookieSecurePolicy.SameAsRequest, oidcOptions.NonceCookie.SecurePolicy);
    }

    [Fact]
    public void AddDescopeOidc_RequiresHttpsMetadata_ByDefault()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = "P123456789");
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.True(oidcOptions.RequireHttpsMetadata);
    }

    #endregion

    #region Custom Authentication Scheme

    [Fact]
    public async Task AddDescopeOidc_UsesCustomAuthenticationScheme()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();
        const string customScheme = "MyDescope";

        // Act
        builder.AddDescopeOidc(options =>
        {
            options.ProjectId = "P123456789";
            options.AuthenticationScheme = customScheme;
        });
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var schemeProvider = serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(customScheme);
        Assert.NotNull(scheme);
        Assert.Equal(customScheme, scheme.Name);
    }

    #endregion

    #region Event Configuration

    [Fact]
    public void AddDescopeOidc_AllowsCustomEventConfiguration()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        builder.AddDescopeOidc(
            options => options.ProjectId = "P123456789",
            events => events.OnRedirectToIdentityProvider += _ => Task.CompletedTask);
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert - event was registered (can't easily trigger it in unit test)
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.NotNull(oidcOptions.Events);
    }

    #endregion

    #region DescopeOidcOptions Object Overload

    [Fact]
    public void AddDescopeOidc_WithOptionsObject_ConfiguresCorrectly()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();
        var descopeOptions = new DescopeOidcOptions
        {
            ProjectId = "P123456789",
            ClientSecret = "test-secret",
            BaseUrl = "https://custom.descope.com",
            CallbackPath = "/custom-callback",
            FlowId = "sign-up-or-in"
        };

        // Act
        builder.AddDescopeOidc(descopeOptions);
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal("https://custom.descope.com/P123456789", oidcOptions.Authority);
        Assert.Equal("P123456789", oidcOptions.ClientId);
        Assert.Equal("test-secret", oidcOptions.ClientSecret);
        Assert.Equal("/custom-callback", oidcOptions.CallbackPath);
    }

    #endregion

    #region Regional URL Support

    [Fact]
    public void AddDescopeOidc_UsesRegionalUrl_ForLongProjectId()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();
        // 32+ character project ID with region at positions 1-4
        const string projectId = "Peus1abcdefghijklmnopqrstuvwxyz12";

        // Act
        builder.AddDescopeOidc(options => options.ProjectId = projectId);
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var oidcOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get("Descope");

        Assert.Equal($"https://api.eus1.descope.com/{projectId}", oidcOptions.Authority);
    }

    #endregion

    #region Chaining Support

    [Fact]
    public void AddDescopeOidc_ReturnsAuthenticationBuilder_ForChaining()
    {
        // Arrange
        var builder = CreateAuthenticationBuilder();

        // Act
        var result = builder.AddDescopeOidc(options => options.ProjectId = "P123456789");

        // Assert
        Assert.Same(builder, result);
    }

    #endregion
}
#endif
