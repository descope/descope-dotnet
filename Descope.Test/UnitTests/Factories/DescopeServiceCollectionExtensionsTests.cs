using Descope;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Descope.Test.UnitTests.Factories;

public class DescopeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDescopeClient_WithoutHttpClientFactoryName_ShouldUseDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key"
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);
        Assert.NotNull(client.Mgmt);
        Assert.NotNull(client.Auth);
    }

    [Fact]
    public void AddDescopeClient_WithCustomHttpClientFactoryName_ShouldUseProvidedName()
    {
        // Arrange
        var services = new ServiceCollection();
        var customName = "MyCustomDescopeClient";
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key",
            HttpClientFactoryName = customName
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);

        // Verify we can also create an HttpClient with the custom name
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
        var httpClient = httpClientFactory.CreateClient(customName);
        Assert.NotNull(httpClient);
    }

    [Fact]
    public void AddDescopeClient_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        DescopeClientOptions options = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddDescopeClient(options));
    }

    [Fact]
    public void AddDescopeClient_WithInvalidOptions_ShouldThrowDescopeException()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "" // Invalid: empty project ID
        };

        // Act & Assert
        Assert.Throws<DescopeException>(() => services.AddDescopeClient(options));
    }

    [Fact]
    public void AddDescopeClient_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key"
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IDescopeClient>());
        Assert.NotNull(serviceProvider.GetService<IHttpClientFactory>());
    }

    [Fact]
    public void AddDescopeClient_WithEmptyHttpClientFactoryName_ShouldUseDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key",
            HttpClientFactoryName = ""
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);

        // Verify the default name "DescopeClient" can be used
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
        var httpClient = httpClientFactory.CreateClient("DescopeClient");
        Assert.NotNull(httpClient);
    }

    [Fact]
    public void AddDescopeClient_WithWhitespaceHttpClientFactoryName_ShouldUseDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key",
            HttpClientFactoryName = "   "
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddDescopeClient_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id"
        };

        // Act
        var result = services.AddDescopeClient(options);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddDescopeClient_WithUnsafeOption_ShouldConfigureHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            IsUnsafe = true
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);
    }
}
