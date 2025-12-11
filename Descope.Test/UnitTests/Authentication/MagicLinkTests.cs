using Descope.Auth;
using Descope.Auth.Models.Onetimev1;
using Descope.Auth.Models.Userv1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Unit tests for Magic Link authentication using the Kiota-based DescopeClient.
/// These tests use a mock request adapter to simulate API responses without making actual HTTP calls.
/// </summary>
public class MagicLinkTests
{
    /// <summary>
    /// Tests that the MagicLink Verify endpoint correctly processes a valid token
    /// and returns the expected authentication response with session JWT, refresh JWT, and user data.
    /// </summary>
    [Fact]
    public async Task MagicLink_Verify_Success()
    {
        // Arrange - Create mock response
        var mockUser = new ResponseUser
        {
            UserId = "user123",
            Email = "test@example.com",
            LoginIds = new List<string> { "test@example.com" }
        };

        var mockResponse = new JWTResponse
        {
            SessionJwt = "session_jwt",
            RefreshJwt = "refresh_jwt",
            User = mockUser
        };

        // Create mock DescopeClient with validation
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<VerifyMagicLinkRequest, JWTResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/magiclink/verify");
            return mockResponse;
        });

        // Act - Call the Verify endpoint
        var request = new VerifyMagicLinkRequest
        {
            Token = "test_token"
        };

        var response = await descopeClient.Auth.V1.Magiclink.Verify.PostAsync(request);

        // Assert - Verify the response
        response.Should().NotBeNull();
        response!.SessionJwt.Should().Be("session_jwt");
        response.RefreshJwt.Should().Be("refresh_jwt");
        response.User.Should().NotBeNull();
        response.User!.UserId.Should().Be("user123");
        response.User.Email.Should().Be("test@example.com");
    }

    /// <summary>
    /// Tests that the MagicLink Verify endpoint correctly deserializes
    /// all expected fields from the JWT response.
    /// </summary>
    [Fact]
    public async Task MagicLink_Verify_DeserializesAllFields()
    {
        // Arrange
        var mockUser = new ResponseUser
        {
            UserId = "user456",
            Email = "another@example.com",
            GivenName = "John",
            FamilyName = "Doe",
            LoginIds = new List<string> { "another@example.com", "john.doe" }
        };

        var mockResponse = new JWTResponse
        {
            SessionJwt = "session_token_abc",
            RefreshJwt = "refresh_token_xyz",
            User = mockUser,
            FirstSeen = true,
            SessionExpiration = 3600,
            CookieDomain = "example.com",
            CookiePath = "/"
        };

        var descopeClient = TestDescopeClientFactory.CreateWithResponse(mockResponse);

        // Act
        var request = new VerifyMagicLinkRequest { Token = "test_token" };
        var response = await descopeClient.Auth.V1.Magiclink.Verify.PostAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!.SessionJwt.Should().Be("session_token_abc");
        response.RefreshJwt.Should().Be("refresh_token_xyz");
        response.FirstSeen.Should().BeTrue();
        response.SessionExpiration.Should().Be(3600);
        response.CookieDomain.Should().Be("example.com");
        response.CookiePath.Should().Be("/");

        response.User.Should().NotBeNull();
        response.User!.UserId.Should().Be("user456");
        response.User.Email.Should().Be("another@example.com");
        response.User.GivenName.Should().Be("John");
        response.User.FamilyName.Should().Be("Doe");
        response.User.LoginIds.Should().HaveCount(2);
        response.User.LoginIds.Should().Contain("another@example.com");
        response.User.LoginIds.Should().Contain("john.doe");
    }

    /// <summary>
    /// Tests that the request body is properly serialized when calling the Verify endpoint.
    /// </summary>
    [Fact]
    public async Task MagicLink_Verify_SendsCorrectRequestBody()
    {
        // Arrange
        VerifyMagicLinkRequest? capturedRequestBody = null;

        var mockResponse = new JWTResponse
        {
            SessionJwt = "test_session",
            RefreshJwt = "test_refresh",
            User = new ResponseUser { UserId = "test_user" }
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<VerifyMagicLinkRequest, JWTResponse>((requestInfo, requestBody) =>
        {
            capturedRequestBody = requestBody;
            return mockResponse;
        });

        // Act
        var request = new VerifyMagicLinkRequest { Token = "my_magic_link_token" };
        await descopeClient.Auth.V1.Magiclink.Verify.PostAsync(request);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.Token.Should().Be("my_magic_link_token");
    }

    /// <summary>
    /// Tests that an ArgumentNullException is thrown when the request body is null.
    /// </summary>
    [Fact]
    public async Task MagicLink_Verify_ThrowsOnNullRequest()
    {
        // Arrange - Create client to test null handling
        var mockResponse = new JWTResponse
        {
            SessionJwt = "test_session",
            RefreshJwt = "test_refresh",
            User = new ResponseUser { UserId = "test_user" }
        };
        var descopeClient = TestDescopeClientFactory.CreateWithResponse(mockResponse);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await descopeClient.Auth.V1.Magiclink.Verify.PostAsync(null!);
        });
    }

    /// <summary>
    /// Tests that the Verify endpoint can handle a minimal response with only required fields.
    /// </summary>
    [Fact]
    public async Task MagicLink_Verify_HandlesMinimalResponse()
    {
        // Arrange - Create response with only essential fields
        var mockResponse = new JWTResponse
        {
            SessionJwt = "minimal_session",
            RefreshJwt = "minimal_refresh",
            User = new ResponseUser
            {
                UserId = "minimal_user"
            }
        };

        var descopeClient = TestDescopeClientFactory.CreateWithResponse(mockResponse);

        // Act
        var request = new VerifyMagicLinkRequest { Token = "test" };
        var response = await descopeClient.Auth.V1.Magiclink.Verify.PostAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!.SessionJwt.Should().Be("minimal_session");
        response.RefreshJwt.Should().Be("minimal_refresh");
        response.User.Should().NotBeNull();
        response.User!.UserId.Should().Be("minimal_user");
    }
}
