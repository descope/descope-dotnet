using System.Net;
using System.Text;
using Descope.Mgmt.Models.Managementv1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Unit tests for the License management API (GET /v1/mgmt/license).
/// </summary>
public class LicenseTests
{
    private const string TestProjectId = "test_project_id";
    private const string TestManagementKey = "test-management-key";

    /// <summary>
    /// Tests that GetAsync sends a GET to /v1/mgmt/license with no query parameters.
    /// </summary>
    [Fact]
    public async Task License_GetAsync_SendsGetRequestToLicenseRoute()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<GetLicenseResponse>(requestInfo =>
        {
            requestInfo.HttpMethod.Should().Be(Method.GET);
            requestInfo.URI.AbsolutePath.Should().Be("/v1/mgmt/license");
            requestInfo.URI.Query.Should().BeEmpty();
            return new GetLicenseResponse { RateLimitTier = "enterprise" };
        });

        // Act
        var result = await descopeClient.Mgmt.V1.License.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result!.RateLimitTier.Should().Be("enterprise");
    }

    /// <summary>
    /// Tests the request on the wire: management key in the Authorization header,
    /// and the JSON response parsed into GetLicenseResponse, ignoring unknown fields.
    /// </summary>
    [Fact]
    public async Task License_GetAsync_UsesManagementKeyAndParsesResponse()
    {
        // Arrange
        var handler = new CapturingHandler("{\"rateLimitTier\":\"enterprise\",\"someNewField\":true}");
        var descopeClient = CreateWithManagementKey(handler);

        // Act
        var result = await descopeClient.Mgmt.V1.License.GetAsync();

        // Assert
        handler.Request.Should().NotBeNull();
        handler.Request!.Method.Should().Be(HttpMethod.Get);
        handler.Request.RequestUri!.AbsolutePath.Should().Be("/v1/mgmt/license");
        handler.Request.RequestUri.Query.Should().BeEmpty();
        handler.Request.Content.Should().BeNull();
        handler.Request.Headers.Authorization!.ToString().Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        result.Should().NotBeNull();
        result!.RateLimitTier.Should().Be("enterprise");
    }

    /// <summary>
    /// Tests that an empty response is parsed leniently.
    /// </summary>
    [Fact]
    public async Task License_GetAsync_ParsesEmptyResponse()
    {
        // Arrange
        var descopeClient = CreateWithManagementKey(new CapturingHandler("{}"));

        // Act
        var result = await descopeClient.Mgmt.V1.License.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result!.RateLimitTier.Should().BeNull();
    }

    private static IDescopeClient CreateWithManagementKey(HttpMessageHandler handler)
    {
        var options = new DescopeClientOptions { ProjectId = TestProjectId, ManagementKey = TestManagementKey, BaseUrl = "https://api.descope.com" };
        var httpClient = new HttpClient(handler);
        var mgmtAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(TestProjectId, TestManagementKey), httpClient: httpClient)
        {
            BaseUrl = options.BaseUrl
        };
        var authAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(TestProjectId, null), httpClient: httpClient)
        {
            BaseUrl = options.BaseUrl
        };
        return DescopeManagementClientFactory.CreateForTest(authAdapter, mgmtAdapter, options, httpClient);
    }

    private class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public CapturingHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            });
        }
    }
}
