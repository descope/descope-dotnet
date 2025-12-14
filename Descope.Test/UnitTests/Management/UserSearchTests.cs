using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Userv1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Xunit;


namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Unit tests for User Search management API.
/// Demonstrates testing endpoints that return collections wrapped in response objects.
/// </summary>
public class UserSearchTests
{
    /// <summary>
    /// Tests that the User Search endpoint correctly returns a collection of users
    /// wrapped in a UsersResponse object.
    /// </summary>
    [Fact]
    public async Task UserSearch_ReturnsUsersCollection()
    {
        // Arrange - Create mock response with collection of users
        var mockUsers = new List<ResponseUser>
        {
            new ResponseUser
            {
                UserId = "user1",
                Email = "user1@example.com",
                LoginIds = new List<string> { "user1@example.com" }
            },
            new ResponseUser
            {
                UserId = "user2",
                Email = "user2@example.com",
                LoginIds = new List<string> { "user2@example.com" }
            },
            new ResponseUser
            {
                UserId = "user3",
                Email = "user3@example.com",
                LoginIds = new List<string> { "user3@example.com" }
            }
        };

        var mockResponse = new UsersResponse
        {
            Users = mockUsers,
            Total = 3
        };

        var descopeClient = TestDescopeClientFactory.CreateWithResponse(mockResponse);

        // Act
        var request = new SearchUsersRequest();
        var response = await descopeClient.Mgmt.V2.User.Search.PostAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!.Total.Should().Be(3);
        response.Users.Should().NotBeNull();
        response.Users.Should().HaveCount(3);
        response.Users![0].UserId.Should().Be("user1");
        response.Users[1].UserId.Should().Be("user2");
        response.Users[2].UserId.Should().Be("user3");
    }

    /// <summary>
    /// Tests that the User Search endpoint can handle an empty result set.
    /// </summary>
    [Fact]
    public async Task UserSearch_ReturnsEmptyCollection()
    {
        // Arrange
        var mockResponse = new UsersResponse
        {
            Users = new List<ResponseUser>(),
            Total = 0
        };

        var descopeClient = TestDescopeClientFactory.CreateWithResponse(mockResponse);

        // Act
        var request = new SearchUsersRequest();
        var response = await descopeClient.Mgmt.V2.User.Search.PostAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!.Total.Should().Be(0);
        response.Users.Should().NotBeNull();
        response.Users.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the User Search endpoint correctly processes the request body.
    /// </summary>
    [Fact]
    public async Task UserSearch_SendsCorrectRequestBody()
    {
        // Arrange
        SearchUsersRequest? capturedRequest = null;

        var mockResponse = new UsersResponse
        {
            Users = new List<ResponseUser>(),
            Total = 0
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<SearchUsersRequest, UsersResponse>(
            (requestInfo, requestBody) =>
            {
                capturedRequest = requestBody;
                requestInfo.HttpMethod.Should().Be(Method.POST);
                requestInfo.URI.AbsolutePath.Should().EndWith("/v2/mgmt/user/search");
                return mockResponse;
            });

        // Act
        var request = new SearchUsersRequest
        {
            Limit = 10,
            Page = 1
        };
        await descopeClient.Mgmt.V2.User.Search.PostAsync(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Limit.Should().Be(10);
        capturedRequest.Page.Should().Be(1);
    }

    /// <summary>
    /// Tests that the User Search endpoint correctly deserializes all user fields.
    /// </summary>
    [Fact]
    public async Task UserSearch_DeserializesAllUserFields()
    {
        // Arrange
        var mockUsers = new List<ResponseUser>
        {
            new ResponseUser
            {
                UserId = "user123",
                Email = "john.doe@example.com",
                GivenName = "John",
                FamilyName = "Doe",
                LoginIds = new List<string> { "john.doe@example.com", "john.doe" },
                Phone = "+1234567890",
                VerifiedEmail = true,
                VerifiedPhone = false
            }
        };

        var mockResponse = new UsersResponse
        {
            Users = mockUsers,
            Total = 1
        };

        var descopeClient = TestDescopeClientFactory.CreateWithResponse(mockResponse);

        // Act
        var request = new SearchUsersRequest();
        var response = await descopeClient.Mgmt.V2.User.Search.PostAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!.Users.Should().HaveCount(1);

        var user = response.Users![0];
        user.UserId.Should().Be("user123");
        user.Email.Should().Be("john.doe@example.com");
        user.GivenName.Should().Be("John");
        user.FamilyName.Should().Be("Doe");
        user.LoginIds.Should().HaveCount(2);
        user.LoginIds.Should().Contain("john.doe@example.com");
        user.LoginIds.Should().Contain("john.doe");
        user.Phone.Should().Be("+1234567890");
        user.VerifiedEmail.Should().BeTrue();
        user.VerifiedPhone.Should().BeFalse();
    }
}
