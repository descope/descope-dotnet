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
/// Requests go through a real Kiota adapter with a capturing HTTP handler, so the
/// HTTP method, route, Authorization header and JSON body are checked on the wire.
/// </summary>
public class AgenticIdentitiesTests
{
    private const string TestProjectId = "test_project_id";
    private const string TestManagementKey = "test_management_key";

    [Fact]
    public async Task AgenticIdentities_Search_SendsRequestAndParsesResponse()
    {
        // Arrange
        const string responseJson = @"{
            ""identities"": [{
                ""consent"": {
                    ""id"": ""consent1"",
                    ""appId"": ""app1"",
                    ""userId"": ""user1"",
                    ""scopes"": [""read"", ""write""],
                    ""grantedBy"": ""admin1"",
                    ""createdTime"": 100,
                    ""tenantId"": ""tenant1"",
                    ""ExpireTime"": 200,
                    ""modifiedTime"": 150
                },
                ""agentName"": ""agent1"",
                ""resourceId"": ""resource1"",
                ""clientId"": ""client1"",
                ""createdBy"": ""creator1""
            }],
            ""total"": 1
        }";
        var handler = new CapturingHandler(responseJson);
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
        handler.Path.Should().Be("/v1/mgmt/agentic/identities/search");
        handler.Authorization.Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        var body = JsonDocument.Parse(handler.Body!).RootElement;
        body.GetProperty("userId").GetString().Should().Be("user1");
        body.GetProperty("clientId").GetString().Should().Be("client1");
        body.GetProperty("resourceId").GetString().Should().Be("resource1");
        body.GetProperty("consentId").GetString().Should().Be("consent1");
        body.GetProperty("page").GetInt32().Should().Be(2);
        body.GetProperty("limit").GetInt32().Should().Be(10);

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
        identity.Consent.Scopes.Should().BeEquivalentTo(new[] { "read", "write" });
        identity.Consent.GrantedBy.Should().Be("admin1");
        identity.Consent.CreatedTime.Should().Be(100);
        identity.Consent.TenantId.Should().Be("tenant1");
        identity.Consent.ExpireTime.Should().Be(200);
        identity.Consent.ModifiedTime.Should().Be(150);
    }

    [Fact]
    public async Task AgenticIdentities_Search_NullRequest_Throws()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        Func<Task> act = async () => await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
        handler.Method.Should().BeNull();
    }

    [Fact]
    public async Task AgenticIdentities_Revoke_SendsRequestAndParsesResponse()
    {
        // Arrange
        var handler = new CapturingHandler(@"{""revoked"": 3}");
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
        handler.Path.Should().Be("/v1/mgmt/agentic/identities/revoke");
        handler.Authorization.Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        var body = JsonDocument.Parse(handler.Body!).RootElement;
        body.GetProperty("userId").GetString().Should().Be("user1");
        body.GetProperty("clientId").GetString().Should().Be("client1");
        body.GetProperty("resourceId").GetString().Should().Be("resource1");
        body.GetProperty("consentId").GetString().Should().Be("consent1");

        // Assert response
        response.Should().NotBeNull();
        response!.Revoked.Should().Be(3);
    }

    [Fact]
    public async Task AgenticIdentities_Revoke_NullRequest_Throws()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        Func<Task> act = async () => await client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
        handler.Method.Should().BeNull();
    }

    private static IDescopeClient CreateClient(HttpMessageHandler handler)
    {
        var options = new DescopeClientOptions
        {
            ProjectId = TestProjectId,
            ManagementKey = TestManagementKey,
            BaseUrl = "https://api.descope.com",
        };
        var httpClient = new HttpClient(handler);
        var mgmtAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(options.ProjectId, options.ManagementKey), httpClient: httpClient)
        {
            BaseUrl = options.BaseUrl
        };
        var authAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(options.ProjectId, null), httpClient: httpClient)
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

        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Path = request.RequestUri?.AbsolutePath;
            Authorization = request.Headers.Authorization?.ToString();
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
