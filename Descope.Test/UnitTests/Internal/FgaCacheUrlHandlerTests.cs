using Descope.Internal;
using Xunit;

namespace Descope.Test.UnitTests.Internal;

public class FgaCacheUrlHandlerTests
{
    private const string BaseUrl = "https://api.descope.com";
    private const string CacheUrl = "https://fga-cache.descope.com";

    [Theory]
    [InlineData("/v1/mgmt/fga/schema")] // SaveSchema
    [InlineData("/v1/mgmt/fga/relations")] // CreateRelations
    [InlineData("/v1/mgmt/fga/relations/delete")] // DeleteRelations
    [InlineData("/v1/mgmt/fga/check")] // Check
    public async Task SendAsync_WithCacheUrlAndCacheEndpointPost_RoutesToCacheUrl(string endpoint)
    {
        // Arrange
        var handler = new FgaCacheUrlHandler(CacheUrl)
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + endpoint);

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(CacheUrl + endpoint, request.RequestUri!.ToString());
    }

    [Theory]
    [InlineData("/v1/mgmt/fga/schema/dryrun")] // DryRunSchema (POST, but not a cache endpoint)
    [InlineData("/v1/mgmt/fga/mappable/schema")] // LoadMappableSchema
    [InlineData("/v1/mgmt/fga/mappable/resources")] // SearchMappableResources
    [InlineData("/v1/mgmt/fga/resources/load")] // LoadResourcesDetails
    [InlineData("/v1/mgmt/fga/resources/save")] // SaveResourcesDetails
    public async Task SendAsync_WithNonCacheEndpoints_UsesBaseUrl(string endpoint)
    {
        // Arrange
        var handler = new FgaCacheUrlHandler(CacheUrl)
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + endpoint);

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(BaseUrl + endpoint, request.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_WithCacheEndpointGetRequest_UsesBaseUrl()
    {
        // Arrange - LoadSchema is a GET request to /v1/mgmt/fga/schema
        var handler = new FgaCacheUrlHandler(CacheUrl)
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/v1/mgmt/fga/schema");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert - Should NOT route to cache URL because it's a GET request
        Assert.Equal(BaseUrl + "/v1/mgmt/fga/schema", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_WithNullCacheUrl_UsesBaseUrl()
    {
        // Arrange
        var handler = new FgaCacheUrlHandler(null)
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/v1/mgmt/fga/check");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(BaseUrl + "/v1/mgmt/fga/check", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_WithEmptyCacheUrl_UsesBaseUrl()
    {
        // Arrange
        var handler = new FgaCacheUrlHandler("")
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/v1/mgmt/fga/check");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(BaseUrl + "/v1/mgmt/fga/check", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_WithQueryParameters_PreservesQueryString()
    {
        // Arrange
        var handler = new FgaCacheUrlHandler(CacheUrl)
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/v1/mgmt/fga/check?param1=value1&param2=value2");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(CacheUrl + "/v1/mgmt/fga/check?param1=value1&param2=value2", request.RequestUri!.ToString());
    }

    [Theory]
    [InlineData("https://cache.example.com")]
    [InlineData("http://192.168.1.1:8080")]
    [InlineData("http://localhost:9000")]
    public async Task SendAsync_WithDifferentCacheUrls_RoutesCorrectly(string cacheUrl)
    {
        // Arrange
        var handler = new FgaCacheUrlHandler(cacheUrl)
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/v1/mgmt/fga/schema");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(cacheUrl + "/v1/mgmt/fga/schema", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendAsync_WithCaseVariationInPath_DoesNotRouteToCache()
    {
        // Arrange - Path matching is case-sensitive
        var handler = new FgaCacheUrlHandler(CacheUrl)
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/v1/mgmt/fga/CHECK"); // Uppercase

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert - Should NOT route to cache URL due to case mismatch
        Assert.Equal(BaseUrl + "/v1/mgmt/fga/CHECK", request.RequestUri!.ToString());
    }

    [Theory]
    [InlineData("https://cache.example.com", "https://cache.example.com")]
    [InlineData("https://cache.example.com/", "https://cache.example.com")]
    [InlineData("http://192.168.1.1:8080/", "http://192.168.1.1:8080")]
    [InlineData("https://fga-cache.descope.com///", "https://fga-cache.descope.com")]
    public async Task Handler_WithTrailingSlashesInCacheUrl_NormalizesAndRoutesCorrectly(string inputCacheUrl, string expectedCacheUrl)
    {
        // Arrange
        var handler = new FgaCacheUrlHandler(inputCacheUrl)
        {
            InnerHandler = new TestHttpMessageHandler()
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/v1/mgmt/fga/check");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert - Handler should normalize trailing slashes
        Assert.Equal(expectedCacheUrl + "/v1/mgmt/fga/check", request.RequestUri!.ToString());
    }

    // Test helper: A simple handler that returns a 200 OK response
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
