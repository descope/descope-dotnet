using Descope.Auth.Models.Onetimev1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Unit tests for the logout extension methods, which require a refresh JWT to be passed explicitly.
/// These tests use a mock request adapter to simulate API responses without making actual HTTP calls.
/// </summary>
public class LogoutTests
{
    private const string TestRefreshJwt = "test_refresh_jwt";

    /// <summary>
    /// Tests that Logout sends the refresh JWT along with the request to the expected endpoint.
    /// </summary>
    [Fact]
    public async Task Logout_PostWithJwt_SendsRefreshJwt()
    {
        // Arrange
        var mockResponse = new JWTResponse();
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<LogoutRequest, JWTResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/logout");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            return mockResponse;
        });

        // Act
        var response = await descopeClient.Auth.V1.Logout.PostWithJwtAsync(new LogoutRequest(), TestRefreshJwt);

        // Assert
        response.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that LogoutAll sends the refresh JWT along with the request to the expected endpoint.
    /// </summary>
    [Fact]
    public async Task LogoutAll_PostWithJwt_SendsRefreshJwt()
    {
        // Arrange
        var mockResponse = new JWTResponse();
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<LogoutRequest, JWTResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/logoutall");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            return mockResponse;
        });

        // Act
        var response = await descopeClient.Auth.V1.Logoutall.PostWithJwtAsync(new LogoutRequest(), TestRefreshJwt);

        // Assert
        response.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that LogoutAll rejects a missing refresh JWT before issuing a request.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task LogoutAll_PostWithJwt_ThrowsWhenRefreshJwtMissing(string? refreshJwt)
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithResponse(new JWTResponse());

        // Act
        Func<Task> act = () => descopeClient.Auth.V1.Logoutall.PostWithJwtAsync(new LogoutRequest(), refreshJwt!);

        // Assert
        await act.Should().ThrowAsync<DescopeException>();
    }

    private static void AssertJwtOption(RequestInformation requestInfo, string expectedJwt)
    {
        var jwtOption = requestInfo.RequestOptions.OfType<DescopeJwtOption>().SingleOrDefault();
        jwtOption.Should().NotBeNull();
        jwtOption!.GetContext()["jwt"].Should().Be(expectedJwt);
    }
}
