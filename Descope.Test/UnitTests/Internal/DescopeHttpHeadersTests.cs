using System.Net.Http;
using Descope;
using Xunit;

namespace Descope.Test.UnitTests.Internal;

public class DescopeHttpHeadersTests
{
    [Fact]
    public void ConfigureHeaders_ShouldAddRequiredHeaders()
    {
        // Arrange
        var httpClient = new HttpClient();
        var projectId = "test_project_id";

        // Act
        DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId);

        // Assert
        Assert.True(httpClient.DefaultRequestHeaders.Contains("x-descope-sdk-name"));
        Assert.True(httpClient.DefaultRequestHeaders.Contains("x-descope-sdk-version"));
        Assert.True(httpClient.DefaultRequestHeaders.Contains("x-descope-sdk-dotnet-version"));
        Assert.True(httpClient.DefaultRequestHeaders.Contains("x-descope-project-id"));
        Assert.Equal(projectId, httpClient.DefaultRequestHeaders.GetValues("x-descope-project-id").Single());
    }

    [Fact]
    public void ConfigureHeaders_CalledTwice_ShouldNotDuplicateHeaders()
    {
        // Arrange
        var httpClient = new HttpClient();
        var projectId = "test_project_id";

        // Act - call ConfigureHeaders twice (this was the bug)
        DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId);
        DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId);

        // Assert - each header should only have one value, not duplicated
        var projectIdValues = httpClient.DefaultRequestHeaders.GetValues("x-descope-project-id").ToList();
        Assert.Single(projectIdValues);
        Assert.Equal(projectId, projectIdValues[0]);

        var sdkNameValues = httpClient.DefaultRequestHeaders.GetValues("x-descope-sdk-name").ToList();
        Assert.Single(sdkNameValues);

        var sdkVersionValues = httpClient.DefaultRequestHeaders.GetValues("x-descope-sdk-version").ToList();
        Assert.Single(sdkVersionValues);

        var dotnetVersionValues = httpClient.DefaultRequestHeaders.GetValues("x-descope-sdk-dotnet-version").ToList();
        Assert.Single(dotnetVersionValues);
    }

    [Fact]
    public void ConfigureHeaders_CalledMultipleTimes_ShouldRemainIdempotent()
    {
        // Arrange
        var httpClient = new HttpClient();
        var projectId = "test_project_id";

        // Act - call ConfigureHeaders many times
        for (int i = 0; i < 10; i++)
        {
            DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId);
        }

        // Assert - headers should still only have single values
        var projectIdValues = httpClient.DefaultRequestHeaders.GetValues("x-descope-project-id").ToList();
        Assert.Single(projectIdValues);
        Assert.Equal(projectId, projectIdValues[0]);
    }

    [Fact]
    public void ConfigureHeaders_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = null!;
        var projectId = "test_project_id";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId));
    }

    [Fact]
    public void ConfigureHeaders_WithNullProjectId_ShouldThrowArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        string projectId = null!;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId));
    }

    [Fact]
    public void ConfigureHeaders_WithEmptyProjectId_ShouldThrowArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var projectId = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId));
    }

    [Fact]
    public void ConfigureHeaders_WithWhitespaceProjectId_ShouldThrowArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var projectId = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DescopeHttpHeaders.ConfigureHeaders(httpClient, projectId));
    }
}
