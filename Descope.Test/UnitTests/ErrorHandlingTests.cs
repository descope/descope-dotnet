using Descope;
using Descope.Test.Helpers;
using Xunit;

namespace Descope.Test.UnitTests;

public class ErrorHandlingTests
{
    [Fact]
    public async Task CreateWithError_ShouldThrowDescopeExceptionWithAllProperties()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithError(
            statusCode: System.Net.HttpStatusCode.Unauthorized,
            errorCode: "E062504",
            errorDescription: "Token expired",
            errorMessage: "Failed to load magic link token"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            // Try to call any endpoint - it should throw at the HTTP level
            await client.Auth.V1.Magiclink.Verify.PostAsync(
                new Descope.Auth.Models.Onetimev1.VerifyMagicLinkRequest
                {
                    Token = "test-token"
                });
        });

        // Assert the exception has all the expected properties
        Assert.Equal("E062504", exception.ErrorCode);
        Assert.Equal("Token expired", exception.ErrorDescription);
        Assert.Equal("Failed to load magic link token", exception.ErrorMessage);
        Assert.Equal("[E062504]: Token expired (Failed to load magic link token)", exception.Message);
    }

    [Fact]
    public async Task CreateWithError_WithoutErrorMessage_ShouldThrowDescopeException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithError(
            statusCode: System.Net.HttpStatusCode.BadRequest,
            errorCode: "E011001",
            errorDescription: "Bad request"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.User.DeletePath.PostAsync(
                new Descope.Mgmt.Models.Managementv1.DeleteUserRequest
                {
                    Identifier = "test@example.com"
                });
        });

        // Assert
        Assert.Equal("E011001", exception.ErrorCode);
        Assert.Equal("Bad request", exception.ErrorDescription);
        Assert.Null(exception.ErrorMessage);
        Assert.Equal("[E011001]: Bad request", exception.Message);
    }

    [Fact]
    public async Task CreateWithEmptyResponse_ShouldNotThrow()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert - should not throw
        await client.Mgmt.V1.User.DeletePath.PostAsync(
            new Descope.Mgmt.Models.Managementv1.DeleteUserRequest
            {
                Identifier = "test@example.com"
            });
    }
}
