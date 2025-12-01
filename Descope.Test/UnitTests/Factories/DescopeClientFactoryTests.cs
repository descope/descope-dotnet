using Descope;
using Descope.Test.Helpers;
using Xunit;

namespace Descope.Test.UnitTests.Factories;

public class DescopeClientFactoryTests
{
    [Fact]
    public void Create()
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key"
        };

        // Act
        var client = DescopeManagementClientFactory.Create(options);

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.Mgmt);
        Assert.NotNull(client.Auth);
    }


    [Fact]
    public void Create_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        DescopeClientOptions options = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DescopeManagementClientFactory.Create(options));
    }

    [Fact]
    public void Create_WithInvalidOptions_ShouldThrowDescopeException()
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = "" // Invalid: empty project ID
        };

        // Act & Assert
        Assert.Throws<DescopeException>(() => DescopeManagementClientFactory.Create(options));
    }

    [Fact]
    public void Create_WithUnsafeOption_ShouldCreateClientSuccessfully()
    {
        // Arrange
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            IsUnsafe = true
        };

        // Act
        var client = DescopeManagementClientFactory.Create(options);

        // Assert
        Assert.NotNull(client);
    }



    [Fact]
    public void CreateForTest_WithValidParameters_ShouldCreateClient()
    {
        // Arrange
        var mockAdapter = MockRequestAdapter.CreateWithEmptyResponse();
        var options = new DescopeClientOptions { ProjectId = "test_project_id" };
        var httpClient = new HttpClient();

        // Act
        var client = DescopeManagementClientFactory.CreateForTest(mockAdapter, mockAdapter, options, httpClient);

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.Mgmt);
        Assert.NotNull(client.Auth);
    }

    [Fact]
    public void CreateForTest_WithNullAuthAdapter_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockAdapter = MockRequestAdapter.CreateWithEmptyResponse();
        var options = new DescopeClientOptions { ProjectId = "test_project_id" };
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DescopeManagementClientFactory.CreateForTest(null!, mockAdapter, options, httpClient));
    }

    [Fact]
    public void CreateForTest_WithNullMgmtAdapter_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockAdapter = MockRequestAdapter.CreateWithEmptyResponse();
        var options = new DescopeClientOptions { ProjectId = "test_project_id" };
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DescopeManagementClientFactory.CreateForTest(mockAdapter, null!, options, httpClient));
    }

    [Fact]
    public void CreateForTest_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockAdapter = MockRequestAdapter.CreateWithEmptyResponse();
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DescopeManagementClientFactory.CreateForTest(mockAdapter, mockAdapter, null!, httpClient));
    }
}
