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

    [Theory]
    [InlineData("https://cache.example.com")]
    [InlineData("http://192.168.1.1:8080")]
    [InlineData("https://fga-cache.descope.com")]
    [InlineData("http://localhost:9000")]
    [InlineData("https://cache.example.com/")] // Trailing slash is valid, will be normalized by middleware
    [InlineData("http://192.168.1.1:8080/")] // Trailing slash is valid, will be normalized by middleware
    [InlineData("https://fga-cache.descope.com///")] // Trailing slashes are valid, will be normalized by middleware
    public void Validate_WithValidFgaCacheUrl_DoesNotThrow(string fgaCacheUrl)
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            FgaCacheUrl = fgaCacheUrl
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Theory]
    [InlineData("ftp://invalid.com")] // Invalid scheme
    [InlineData("not-a-url")] // Not a URL
    [InlineData("javascript:alert(1)")] // Invalid scheme
    [InlineData("file:///path/to/file")] // File scheme not allowed
    [InlineData("//example.com")] // Protocol-relative URL
    public void Validate_WithInvalidFgaCacheUrl_ThrowsDescopeException(string invalidUrl)
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            FgaCacheUrl = invalidUrl
        };

        // Act & Assert
        var exception = Assert.Throws<DescopeException>(() => options.Validate());
        Assert.Contains("FgaCacheUrl must be a valid HTTP or HTTPS URL", exception.Message);
    }

    [Fact]
    public void Validate_WithNullFgaCacheUrl_DoesNotThrow()
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            FgaCacheUrl = null
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithEmptyFgaCacheUrl_DoesNotThrow()
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            FgaCacheUrl = ""
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }
}
