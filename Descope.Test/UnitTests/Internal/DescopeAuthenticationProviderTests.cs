using Microsoft.Kiota.Abstractions;
using Xunit;

namespace Descope.Test.UnitTests.Internal;

/// <summary>
/// Unit tests for DescopeAuthenticationProvider, verifying correct bearer token generation
/// for various authentication scenarios including AuthManagementKey support.
/// </summary>
public class DescopeAuthenticationProviderTests
{
    private const string TestProjectId = "test-project-id";
    private const string TestManagementKey = "test-management-key";
    private const string TestAuthManagementKey = "test-auth-management-key";
    private const string TestJwt = "test.jwt.token";
    private const string TestAccessKey = "test.access.key";

    [Fact]
    public async Task AuthenticateRequestAsync_ManagementProvider_ShouldGenerateCorrectBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId, TestManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.GET,
            UrlTemplate = "https://api.descope.com/v1/mgmt/test"
        };

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        Assert.Equal($"Bearer {TestProjectId}:{TestManagementKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AuthProviderWithoutJwt_ShouldGenerateProjectIdOnlyBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/otp/verify"
        };

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        Assert.Equal($"Bearer {TestProjectId}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AuthProviderWithJwt_ShouldGenerateProjectIdAndJwtBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/refresh"
        };
        requestInfo.AddRequestOptions(new[] { DescopeJwtOption.WithJwt(TestJwt) });

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        Assert.Equal($"Bearer {TestProjectId}:{TestJwt}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AuthProviderWithJwtAndAuthManagementKey_ShouldGenerateFullBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, TestAuthManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/refresh"
        };
        requestInfo.AddRequestOptions(new[] { DescopeJwtOption.WithJwt(TestJwt) });

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        Assert.Equal($"Bearer {TestProjectId}:{TestJwt}:{TestAuthManagementKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AuthProviderWithAuthManagementKeyButNoJwt_ShouldGenerateProjectIdAndAuthManagementKeyBearerToken()
    {
        // Arrange
        // AuthManagementKey should be appended even without JWT
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, TestAuthManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/otp/verify"
        };

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        // AuthManagementKey should be included even when there's no JWT
        Assert.Equal($"Bearer {TestProjectId}:{TestAuthManagementKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AuthProviderWithJwtViaAdditionalContext_ShouldGenerateProjectIdAndJwtBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/refresh"
        };
        var additionalContext = new Dictionary<string, object>
        {
            { "jwt", TestJwt }
        };

        // Act
        await provider.AuthenticateRequestAsync(requestInfo, additionalContext);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        Assert.Equal($"Bearer {TestProjectId}:{TestJwt}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AuthProviderWithJwtViaAdditionalContextAndAuthManagementKey_ShouldGenerateFullBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, TestAuthManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/refresh"
        };
        var additionalContext = new Dictionary<string, object>
        {
            { "jwt", TestJwt }
        };

        // Act
        await provider.AuthenticateRequestAsync(requestInfo, additionalContext);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        Assert.Equal($"Bearer {TestProjectId}:{TestJwt}:{TestAuthManagementKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AuthProviderWithEmptyJwt_ShouldGenerateProjectIdAndAuthManagementKeyBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, TestAuthManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/refresh"
        };
        var additionalContext = new Dictionary<string, object>
        {
            { "jwt", "" } // Empty JWT
        };

        // Act
        await provider.AuthenticateRequestAsync(requestInfo, additionalContext);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        // Empty JWT should result in projectId:authManagementKey
        Assert.Equal($"Bearer {TestProjectId}:{TestAuthManagementKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_NullProjectId_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.Throws<ArgumentNullException>(() => new DescopeAuthenticationProvider(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public async Task AuthenticateRequestAsync_NullRequestInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId);

        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await provider.AuthenticateRequestAsync(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public async Task AuthenticateRequestAsync_DescopeJwtOptionTakesPrecedenceOverAdditionalContext()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, TestAuthManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/refresh"
        };

        const string jwtFromOption = "jwt.from.option";
        const string jwtFromContext = "jwt.from.context";

        requestInfo.AddRequestOptions(new[] { DescopeJwtOption.WithJwt(jwtFromOption) });
        var additionalContext = new Dictionary<string, object>
        {
            { "jwt", jwtFromContext }
        };

        // Act
        await provider.AuthenticateRequestAsync(requestInfo, additionalContext);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        // DescopeJwtOption should take precedence
        Assert.Equal($"Bearer {TestProjectId}:{jwtFromOption}:{TestAuthManagementKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_WhitespaceAuthManagementKey_ShouldNotAppendToToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, "   ");
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/refresh"
        };
        requestInfo.AddRequestOptions(new[] { DescopeJwtOption.WithJwt(TestJwt) });

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        // Whitespace AuthManagementKey should be treated as not present
        Assert.Equal($"Bearer {TestProjectId}:{TestJwt}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AccessKeyWithoutAuthManagementKey_ShouldGenerateProjectIdAndKeyBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/accesskey/exchange"
        };
        requestInfo.AddRequestOptions(new[] { DescopeKeyOption.WithKey(TestAccessKey) });

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        Assert.Equal($"Bearer {TestProjectId}:{TestAccessKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_AccessKeyWithAuthManagementKey_ShouldNotAppendAuthManagementKey()
    {
        // Arrange
        // AuthManagementKey should NOT be appended when using access key authentication
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, TestAuthManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/accesskey/exchange"
        };
        requestInfo.AddRequestOptions(new[] { DescopeKeyOption.WithKey(TestAccessKey) });

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        // AuthManagementKey should NOT be included for access key authentication
        Assert.Equal($"Bearer {TestProjectId}:{TestAccessKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_EmptyAccessKey_ShouldGenerateProjectIdOnlyBearerToken()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, TestAuthManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/accesskey/exchange"
        };
        requestInfo.AddRequestOptions(new[] { DescopeKeyOption.WithKey("") });

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        // Empty access key should result in projectId:authManagementKey (since key auth is not triggered)
        Assert.Equal($"Bearer {TestProjectId}:{TestAuthManagementKey}", authHeader);
    }

    [Fact]
    public async Task AuthenticateRequestAsync_KeyOptionTakesPrecedenceOverJwtOption()
    {
        // Arrange
        var provider = new DescopeAuthenticationProvider(TestProjectId, null, TestAuthManagementKey);
        var requestInfo = new RequestInformation
        {
            HttpMethod = Method.POST,
            UrlTemplate = "https://api.descope.com/v1/auth/accesskey/exchange"
        };
        // Add both key and JWT options - key should take precedence
        requestInfo.AddRequestOptions(new IRequestOption[]
        {
            DescopeKeyOption.WithKey(TestAccessKey),
            DescopeJwtOption.WithJwt(TestJwt)
        });

        // Act
        await provider.AuthenticateRequestAsync(requestInfo);

        // Assert
        Assert.True(requestInfo.Headers.ContainsKey("Authorization"));
        var authHeader = string.Join(", ", requestInfo.Headers["Authorization"]);
        // Key option should take precedence and authManagementKey should NOT be appended
        Assert.Equal($"Bearer {TestProjectId}:{TestAccessKey}", authHeader);
    }
}
