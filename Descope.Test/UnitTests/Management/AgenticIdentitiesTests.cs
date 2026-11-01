using System.Net;
using System.Text;
using System.Text.Json;
using Descope.Mgmt.Models.Managementv1;
using FluentAssertions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Unit tests for the agentic identities management API (search and revoke).
/// These tests run through the real Kiota HTTP stack with a capturing handler,
/// so they verify the HTTP method, route, Authorization header and JSON body.
/// </summary>
public class AgenticIdentitiesTests
{
    private const string TestProjectId = "test_project_id";
    private const string TestManagementKey = "test_management_key";
    private const string TestBaseUrl = "https://api.descope.test";

    private const string SearchResponseJson = @"{
        ""identities"": [{
            ""consent"": {
                ""id"": ""consent1"",
                ""appId"": ""app1"",
                ""userId"": ""user1"",
                ""scopes"": [""read"", ""write""],
                ""grantedBy"": ""granter1"",
                ""createdTime"": 100,
                ""tenantId"": ""tenant1"",
                ""ExpireTime"": 200,
                ""modifiedTime"": 300,
                ""authMethod"": ""idJag""
            },
            ""agentName"": ""agent1"",
            ""resourceId"": ""resource1"",
            ""clientId"": ""client1"",
            ""createdBy"": ""creator1""
        }],
        ""total"": 1
    }";

    [Fact]
    public async Task SearchAgenticIdentities_SendsRequestAndParsesResponse()
    {
        // Arrange
        var handler = new CapturingHandler(SearchResponseJson);
        var client = CreateClient(handler);

        // Act
        var response = await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(new SearchAgenticIdentitiesRequest
        {
            UserId = "user1",
            ClientId = "client1",
            ResourceId = "resource1",
            ConsentId = "consent1",
            Page = 2,
            Limit = 10,
        });

        // Assert request
        handler.Method.Should().Be(HttpMethod.Post);
        handler.Uri!.AbsolutePath.Should().Be("/v1/mgmt/agentic/identities/search");
        handler.Authorization.Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        using (var body = JsonDocument.Parse(handler.Body!))
        {
            var root = body.RootElement;
            root.GetProperty("userId").GetString().Should().Be("user1");
            root.GetProperty("clientId").GetString().Should().Be("client1");
            root.GetProperty("resourceId").GetString().Should().Be("resource1");
            root.GetProperty("consentId").GetString().Should().Be("consent1");
            root.GetProperty("page").GetInt32().Should().Be(2);
            root.GetProperty("limit").GetInt32().Should().Be(10);
        }

        // Assert response
        response.Should().NotBeNull();
        response!.Total.Should().Be(1);
        response.Identities.Should().HaveCount(1);
        var identity = response.Identities![0];
        identity.AgentName.Should().Be("agent1");
        identity.ResourceId.Should().Be("resource1");
        identity.ClientId.Should().Be("client1");
        identity.CreatedBy.Should().Be("creator1");
        identity.Consent.Should().NotBeNull();
        identity.Consent!.Id.Should().Be("consent1");
        identity.Consent.AppId.Should().Be("app1");
        identity.Consent.UserId.Should().Be("user1");
        identity.Consent.Scopes.Should().Equal("read", "write");
        identity.Consent.GrantedBy.Should().Be("granter1");
        identity.Consent.CreatedTime.Should().Be(100);
        identity.Consent.TenantId.Should().Be("tenant1");
        identity.Consent.ExpireTime.Should().Be(200);
        identity.Consent.ModifiedTime.Should().Be(300);
        identity.Consent.AuthMethod.Should().Be("idJag");
    }

    [Fact]
    public async Task SearchAgenticIdentities_EmptyRequest_OmitsUnsetFieldsAndHandlesNullConsent()
    {
        // Arrange
        var handler = new CapturingHandler(@"{""identities"":[{""agentName"":""agent1""}]}");
        var client = CreateClient(handler);

        // Act
        var response = await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(new SearchAgenticIdentitiesRequest());

        // Assert
        handler.Body.Should().Be("{}");
        response.Should().NotBeNull();
        response!.Total.Should().BeNull();
        response.Identities.Should().HaveCount(1);
        response.Identities![0].Consent.Should().BeNull();
    }

    [Fact]
    public async Task SearchAgenticIdentities_NullRequest_ThrowsBeforeSending()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.Mgmt.V1.Agentic.Identities.Search.PostAsync(null!));
        handler.Method.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAgenticIdentities_SendsRequestAndParsesResponse()
    {
        // Arrange
        var handler = new CapturingHandler(@"{""revoked"":3}");
        var client = CreateClient(handler);

        // Act
        var response = await client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(new RevokeAgenticIdentitiesRequest
        {
            UserId = "user1",
            ClientId = "client1",
            ResourceId = "resource1",
            ConsentId = "consent1",
        });

        // Assert request
        handler.Method.Should().Be(HttpMethod.Post);
        handler.Uri!.AbsolutePath.Should().Be("/v1/mgmt/agentic/identities/revoke");
        handler.Authorization.Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        using (var body = JsonDocument.Parse(handler.Body!))
        {
            var root = body.RootElement;
            root.GetProperty("userId").GetString().Should().Be("user1");
            root.GetProperty("clientId").GetString().Should().Be("client1");
            root.GetProperty("resourceId").GetString().Should().Be("resource1");
            root.GetProperty("consentId").GetString().Should().Be("consent1");
        }

        // Assert response
        response.Should().NotBeNull();
        response!.Revoked.Should().Be(3);
    }

    [Fact]
    public async Task RevokeAgenticIdentities_NullRequest_ThrowsBeforeSending()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(null!));
        handler.Method.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAgenticIdentities_ErrorResponse_ThrowsDescopeException()
    {
        var client = Helpers.TestDescopeClientFactory.CreateWithError(
            statusCode: HttpStatusCode.BadRequest,
            errorCode: "E011001",
            errorDescription: "Bad request");

        var exception = await Assert.ThrowsAsync<DescopeException>(() =>
            client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(new RevokeAgenticIdentitiesRequest()));
        exception.ErrorCode.Should().Be("E011001");
    }

    private static IDescopeClient CreateClient(CapturingHandler handler)
    {
        var options = new DescopeClientOptions
        {
            ProjectId = TestProjectId,
            ManagementKey = TestManagementKey,
            BaseUrl = TestBaseUrl,
        };
        var httpClient = new HttpClient(handler);
        var mgmtAdapter = new HttpClientRequestAdapter(
            new DescopeAuthenticationProvider(TestProjectId, TestManagementKey), httpClient: httpClient)
        {
            BaseUrl = TestBaseUrl,
        };
        var authAdapter = new HttpClientRequestAdapter(
            new DescopeAuthenticationProvider(TestProjectId, null), httpClient: httpClient)
        {
            BaseUrl = TestBaseUrl,
        };
        return DescopeManagementClientFactory.CreateForTest(authAdapter, mgmtAdapter, options, httpClient);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public CapturingHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json"),
            };
        }
    }
}
