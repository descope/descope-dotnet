using Xunit;

namespace Descope.Test.UnitTests;

public class DescopeClientOptionsTests
{
    [Theory]
    [InlineData("Puse1567890123456789012345678901", "https://api.use1.descope.com")] // 32 chars, region: use1
    [InlineData("Peuc12gotLL7ZROo6LTAOK1XGv0cqsqX", "https://api.euc1.descope.com")] // 32 chars, region: euc1
    [InlineData("Pus01234567890123456789012345678901", "https://api.us01.descope.com")] // 34 chars
    [InlineData("PtestABCDEFGHIJKLMNOPQRSTUVWXYZ123", "https://api.test.descope.com")] // 34 chars
    [InlineData("Pabcd1234567890123456789012345678901234567890", "https://api.abcd.descope.com")] // Long project ID
    public void GetBaseUrlForProjectId_WithLongProjectId_ReturnsRegionalUrl(string projectId, string expectedUrl)
    {
        // Act
        var result = DescopeClientOptions.GetBaseUrlForProjectId(projectId);

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    [Theory]
    [InlineData("short")] // Too short
    [InlineData("P12345678901234567890123456789")] // 31 chars, just under threshold
    [InlineData("P1234567890123456789012345678")] // 29 chars
    [InlineData("P2STT68fqIBuVDoQf42Pvosh0jxi")] // obfuscated prod ID
    [InlineData("test")] // 4 chars
    public void GetBaseUrlForProjectId_WithShortProjectId_ReturnsDefaultUrl(string projectId)
    {
        // Arrange
        const string expectedUrl = "https://api.descope.com";

        // Act
        var result = DescopeClientOptions.GetBaseUrlForProjectId(projectId);

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetBaseUrlForProjectId_WithNullOrEmpty_ReturnsDefaultUrl(string? projectId)
    {
        // Arrange
        const string expectedUrl = "https://api.descope.com";

        // Act
        var result = DescopeClientOptions.GetBaseUrlForProjectId(projectId!);

        // Assert
        Assert.Equal(expectedUrl, result);
    }

    [Fact]
    public void Validate_WithValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id"
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithEmptyProjectId_ThrowsDescopeException()
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = ""
        };

        // Act & Assert
        var exception = Assert.Throws<DescopeException>(() => options.Validate());
        Assert.Contains("ProjectId is required", exception.Message);
    }
}
