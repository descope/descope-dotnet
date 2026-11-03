using System.Net;
using System.Text;
using System.Text.Json;
using Descope;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Projectv1;
using Descope.Mgmt.Models.Searchv1;
using FluentAssertions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;
using SortField = Descope.Mgmt.Models.Projectv1.SortField;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Unit tests for the agentic management APIs (/v1/mgmt/agentic/...): agentic identities and
/// Agentic Governance Suite settings and access requests.
/// Requests go through a real Kiota HTTP adapter with a capturing handler, so the HTTP method,
/// route, Authorization header and JSON body are all checked.
/// </summary>
public class AgenticTests
{
    private const string ProjectId = "test_project_id";
    private const string ManagementKey = "test_management_key";
    private const string BaseUrl = "https://api.descope.com";

    private const string GovernanceAccessRequestJson = @"{
        ""id"": ""req-1"",
        ""requestorUserId"": ""user-1"",
        ""requestorName"": ""Requestor"",
        ""clientType"": ""agent"",
        ""grantTypes"": [""clientCredentials""],
        ""targets"": [{""name"": ""mcp"", ""kind"": ""resource"", ""targetId"": ""res-1"", ""scopes"": [""read""], ""allScopes"": true}],
        ""scopes"": [""openid""],
        ""justification"": ""because"",
        ""status"": ""approved"",
        ""resolvedByUserId"": ""admin-1"",
        ""resolvedByName"": ""Admin"",
        ""resolutionNote"": ""ok"",
        ""clientId"": ""client-1"",
        ""clientAppId"": ""app-1"",
        ""createdTime"": ""1700000000"",
        ""modifiedTime"": ""1700000001"",
        ""version"": ""3"",
        ""clientName"": ""My agent"",
        ""clientDeleted"": true
    }";

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public int Calls { get; private set; }

        public CapturingHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Request = request;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    private static IDescopeClient CreateClient(CapturingHandler handler)
    {
        var options = new DescopeClientOptions { ProjectId = ProjectId, ManagementKey = ManagementKey, BaseUrl = BaseUrl };
        var httpClient = new HttpClient(handler);
        var mgmtAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(ProjectId, ManagementKey), httpClient: httpClient) { BaseUrl = BaseUrl };
        var authAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(ProjectId, null), httpClient: httpClient) { BaseUrl = BaseUrl };
        return DescopeManagementClientFactory.CreateForTest(authAdapter, mgmtAdapter, options, httpClient);
    }

    private static void AssertRequest(CapturingHandler handler, HttpMethod method, string path)
    {
        handler.Calls.Should().Be(1);
        handler.Request!.Method.Should().Be(method);
        handler.Request.RequestUri!.AbsolutePath.Should().Be(path);
        handler.Request.Headers.Authorization!.ToString().Should().Be($"Bearer {ProjectId}:{ManagementKey}");
    }

    private static JsonElement BodyOf(CapturingHandler handler)
    {
        handler.Body.Should().NotBeNullOrEmpty();
        return JsonDocument.Parse(handler.Body!).RootElement;
    }

    private static void AssertFullGovernanceAccessRequest(GovernanceAccessRequest? r)
    {
        r.Should().NotBeNull();
        r!.Id.Should().Be("req-1");
        r.RequestorUserId.Should().Be("user-1");
        r.RequestorName.Should().Be("Requestor");
        r.ClientType.Should().Be("agent");
        r.GrantTypes.Should().Equal("clientCredentials");
        r.Targets.Should().HaveCount(1);
        r.Targets![0].Name.Should().Be("mcp");
        r.Targets[0].Kind.Should().Be("resource");
        r.Targets[0].TargetId.Should().Be("res-1");
        r.Targets[0].Scopes.Should().Equal("read");
        r.Targets[0].AllScopes.Should().BeTrue();
        r.Scopes.Should().Equal("openid");
        r.Justification.Should().Be("because");
        r.Status.Should().Be("approved");
        r.ResolvedByUserId.Should().Be("admin-1");
        r.ResolvedByName.Should().Be("Admin");
        r.ResolutionNote.Should().Be("ok");
        r.ClientId.Should().Be("client-1");
        r.ClientAppId.Should().Be("app-1");
        r.CreatedTime.Should().Be("1700000000");
        r.ModifiedTime.Should().Be("1700000001");
        r.Version.Should().Be("3");
        r.ClientName.Should().Be("My agent");
        r.ClientDeleted.Should().BeTrue();
    }

    // ---------- Agentic identities ----------

    [Fact]
    public async Task SearchAgenticIdentities_SendsRequestAndParsesResponse()
    {
        var handler = new CapturingHandler(@"{
            ""identities"": [{
                ""consent"": {""id"": ""c-1"", ""appId"": ""app-1"", ""userId"": ""user-1"", ""scopes"": [""read""], ""grantedBy"": ""user-1"",
                              ""createdTime"": 10, ""tenantId"": ""t-1"", ""ExpireTime"": 20, ""modifiedTime"": 30, ""authMethod"": ""idJag""},
                ""agentName"": ""Agent"", ""resourceId"": ""res-1"", ""clientId"": ""client-1"", ""createdBy"": ""admin""
            }],
            ""total"": 1
        }");
        var client = CreateClient(handler);

        var response = await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(new SearchAgenticIdentitiesRequest
        {
            UserId = "user-1",
            ClientId = "client-1",
            ResourceId = "res-1",
            ConsentId = "c-1",
            Page = 2,
            Limit = 5,
        });

        AssertRequest(handler, HttpMethod.Post, "/v1/mgmt/agentic/identities/search");
        var body = BodyOf(handler);
        body.GetProperty("userId").GetString().Should().Be("user-1");
        body.GetProperty("clientId").GetString().Should().Be("client-1");
        body.GetProperty("resourceId").GetString().Should().Be("res-1");
        body.GetProperty("consentId").GetString().Should().Be("c-1");
        body.GetProperty("page").GetInt32().Should().Be(2);
        body.GetProperty("limit").GetInt32().Should().Be(5);

        response.Should().NotBeNull();
        response!.Total.Should().Be(1);
        response.Identities.Should().HaveCount(1);
        var identity = response.Identities![0];
        identity.AgentName.Should().Be("Agent");
        identity.ResourceId.Should().Be("res-1");
        identity.ClientId.Should().Be("client-1");
        identity.CreatedBy.Should().Be("admin");
        identity.Consent!.Id.Should().Be("c-1");
        identity.Consent.AppId.Should().Be("app-1");
        identity.Consent.UserId.Should().Be("user-1");
        identity.Consent.Scopes.Should().Equal("read");
        identity.Consent.GrantedBy.Should().Be("user-1");
        identity.Consent.CreatedTime.Should().Be(10);
        identity.Consent.TenantId.Should().Be("t-1");
        identity.Consent.ExpireTime.Should().Be(20);
        identity.Consent.ModifiedTime.Should().Be(30);
        identity.Consent.AuthMethod.Should().Be("idJag");
    }

    [Fact]
    public async Task SearchAgenticIdentities_ThrowsWhenRequestIsNull()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Mgmt.V1.Agentic.Identities.Search.PostAsync(null!));
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task RevokeAgenticIdentities_SendsRequestAndParsesResponse()
    {
        var handler = new CapturingHandler(@"{""revoked"": 4}");
        var client = CreateClient(handler);

        var response = await client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(new RevokeAgenticIdentitiesRequest
        {
            UserId = "user-1",
            ClientId = "client-1",
            ResourceId = "res-1",
            ConsentId = "c-1",
        });

        AssertRequest(handler, HttpMethod.Post, "/v1/mgmt/agentic/identities/revoke");
        var body = BodyOf(handler);
        body.GetProperty("userId").GetString().Should().Be("user-1");
        body.GetProperty("clientId").GetString().Should().Be("client-1");
        body.GetProperty("resourceId").GetString().Should().Be("res-1");
        body.GetProperty("consentId").GetString().Should().Be("c-1");
        response!.Revoked.Should().Be(4);
    }

    [Fact]
    public async Task RevokeAgenticIdentities_ThrowsWhenRequestIsNull()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(null!));
        handler.Calls.Should().Be(0);
    }

    // ---------- Governance settings ----------

    [Fact]
    public async Task GetGovernanceSettings_SendsRequestAndParsesResponse()
    {
        var handler = new CapturingHandler(@"{""configured"": true, ""autoApproval"": true, ""suiteDisabled"": false}");
        var client = CreateClient(handler);

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.GetAsync();

        AssertRequest(handler, HttpMethod.Get, "/v1/mgmt/agentic/governance/settings");
        handler.Request!.RequestUri!.Query.Should().BeEmpty();
        response!.Configured.Should().BeTrue();
        response.AutoApproval.Should().BeTrue();
        response.SuiteDisabled.Should().BeFalse();
    }

    [Fact]
    public async Task SetGovernanceSettings_SendsRequestAndParsesResponse()
    {
        var handler = new CapturingHandler(@"{""configured"": true, ""autoApproval"": false, ""suiteDisabled"": true}");
        var client = CreateClient(handler);

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.PostAsync(new AgenticGovernanceSettings
        {
            Configured = true,
            AutoApproval = false,
            SuiteDisabled = true,
        });

        AssertRequest(handler, HttpMethod.Post, "/v1/mgmt/agentic/governance/settings");
        var body = BodyOf(handler);
        body.GetProperty("configured").GetBoolean().Should().BeTrue();
        body.GetProperty("autoApproval").GetBoolean().Should().BeFalse();
        body.GetProperty("suiteDisabled").GetBoolean().Should().BeTrue();
        response!.Configured.Should().BeTrue();
        response.AutoApproval.Should().BeFalse();
        response.SuiteDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetGovernanceSettings_OmitsUnsetFields()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await client.Mgmt.V1.Agentic.Governance.Settings.PostAsync(new AgenticGovernanceSettings { AutoApproval = true });

        var body = BodyOf(handler);
        body.GetProperty("autoApproval").GetBoolean().Should().BeTrue();
        body.TryGetProperty("configured", out _).Should().BeFalse();
        body.TryGetProperty("suiteDisabled", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SetGovernanceSettings_ThrowsWhenRequestIsNull()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Mgmt.V1.Agentic.Governance.Settings.PostAsync(null!));
        handler.Calls.Should().Be(0);
    }

    // ---------- Governance access requests: create ----------

    private static CreateGovernanceAccessRequestRequest NewCreateRequest() => new()
    {
        ClientName = "My agent",
        ClientType = "agent",
        GrantTypes = new List<string> { "clientCredentials", "jwtBearer" },
        Targets = new List<GovernanceAccessRequestTarget>
        {
            new() { Name = "mcp", Kind = "resource", Scopes = new List<string> { "read" }, AllScopes = false },
        },
        Scopes = new List<string> { "openid" },
        Justification = "because",
    };

    [Fact]
    public async Task CreateGovernanceAccessRequest_SendsRequestAndParsesResponse()
    {
        var handler = new CapturingHandler(GovernanceAccessRequestJson);
        var client = CreateClient(handler);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithRequestorUserIdAsync("user-1", NewCreateRequest());

        AssertRequest(handler, HttpMethod.Post, "/v1/mgmt/agentic/governance/requests/create");
        var body = BodyOf(handler);
        body.GetProperty("requestorUserId").GetString().Should().Be("user-1");
        body.GetProperty("clientName").GetString().Should().Be("My agent");
        body.GetProperty("clientType").GetString().Should().Be("agent");
        body.GetProperty("grantTypes").EnumerateArray().Select(e => e.GetString()).Should().Equal("clientCredentials", "jwtBearer");
        var target = body.GetProperty("targets")[0];
        target.GetProperty("name").GetString().Should().Be("mcp");
        target.GetProperty("kind").GetString().Should().Be("resource");
        target.GetProperty("scopes")[0].GetString().Should().Be("read");
        target.GetProperty("allScopes").GetBoolean().Should().BeFalse();
        body.GetProperty("scopes")[0].GetString().Should().Be("openid");
        body.GetProperty("justification").GetString().Should().Be("because");

        AssertFullGovernanceAccessRequest(response);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateGovernanceAccessRequest_ThrowsWhenRequestorUserIdIsMissing(string? requestorUserId)
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<DescopeException>(() =>
            client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithRequestorUserIdAsync(requestorUserId!, NewCreateRequest()));
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task CreateGovernanceAccessRequest_ThrowsWhenRequestIsNull()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<DescopeException>(() =>
            client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithRequestorUserIdAsync("user-1", null!));
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task CreateGovernanceAccessRequest_ThrowsWhenGrantTypeIsEmpty()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);
        var request = NewCreateRequest();
        request.GrantTypes = new List<string> { "clientCredentials", "" };

        await Assert.ThrowsAsync<DescopeException>(() =>
            client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithRequestorUserIdAsync("user-1", request));
        handler.Calls.Should().Be(0);
    }

    // ---------- Governance access requests: get ----------

    [Fact]
    public async Task GetGovernanceAccessRequest_SendsRequestAndParsesResponse()
    {
        var handler = new CapturingHandler(GovernanceAccessRequestJson);
        var client = CreateClient(handler);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.GetPath.PostWithIdAsync("req-1");

        AssertRequest(handler, HttpMethod.Post, "/v1/mgmt/agentic/governance/requests/get");
        BodyOf(handler).GetProperty("id").GetString().Should().Be("req-1");
        AssertFullGovernanceAccessRequest(response);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetGovernanceAccessRequest_ThrowsWhenIdIsMissing(string? id)
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.GetPath.PostWithIdAsync(id!));
        handler.Calls.Should().Be(0);
    }

    // ---------- Governance access requests: resolve ----------

    [Fact]
    public async Task ResolveGovernanceAccessRequest_SendsRequestAndParsesResponse()
    {
        var handler = new CapturingHandler(GovernanceAccessRequestJson);
        var client = CreateClient(handler);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithIdAsync("req-1", "approve", "ok");

        AssertRequest(handler, HttpMethod.Post, "/v1/mgmt/agentic/governance/requests/resolve");
        var body = BodyOf(handler);
        body.GetProperty("id").GetString().Should().Be("req-1");
        body.GetProperty("action").GetString().Should().Be("approve");
        body.GetProperty("resolutionNote").GetString().Should().Be("ok");
        AssertFullGovernanceAccessRequest(response);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ResolveGovernanceAccessRequest_ThrowsWhenIdIsMissing(string? id)
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<DescopeException>(() =>
            client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithIdAsync(id!, "approve"));
        handler.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ResolveGovernanceAccessRequest_ThrowsWhenActionIsMissing(string? action)
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<DescopeException>(() =>
            client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithIdAsync("req-1", action!));
        handler.Calls.Should().Be(0);
    }

    // ---------- Governance access requests: search ----------

    [Fact]
    public async Task SearchGovernanceAccessRequests_SendsRequestAndParsesResponse()
    {
        var handler = new CapturingHandler($@"{{""requests"": [{GovernanceAccessRequestJson}], ""total"": 1}}");
        var client = CreateClient(handler);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(new SearchGovernanceAccessRequestsRequest
        {
            Limit = 10,
            Page = 1,
            SearchFields = new List<SearchField>
            {
                new() { Field = "status", ValArr = new List<string> { "pending" }, ValStr = "pending", Negative = true, ValType = "string" },
            },
            Sort = new List<SortField> { new() { Field = "createdTime", Desc = true } },
            Text = "agent",
        });

        AssertRequest(handler, HttpMethod.Post, "/v1/mgmt/agentic/governance/requests/search");
        var body = BodyOf(handler);
        body.GetProperty("limit").GetInt32().Should().Be(10);
        body.GetProperty("page").GetInt32().Should().Be(1);
        var searchField = body.GetProperty("searchFields")[0];
        searchField.GetProperty("field").GetString().Should().Be("status");
        searchField.GetProperty("valArr")[0].GetString().Should().Be("pending");
        searchField.GetProperty("valStr").GetString().Should().Be("pending");
        searchField.GetProperty("negative").GetBoolean().Should().BeTrue();
        searchField.GetProperty("valType").GetString().Should().Be("string");
        var sort = body.GetProperty("sort")[0];
        sort.GetProperty("field").GetString().Should().Be("createdTime");
        sort.GetProperty("desc").GetBoolean().Should().BeTrue();
        body.GetProperty("text").GetString().Should().Be("agent");

        response!.Total.Should().Be(1);
        response.Requests.Should().HaveCount(1);
        AssertFullGovernanceAccessRequest(response.Requests![0]);
    }

    [Fact]
    public async Task SearchGovernanceAccessRequests_ThrowsWhenRequestIsNull()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<DescopeException>(() =>
            client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(null!));
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task SearchGovernanceAccessRequests_ThrowsWhenSortFieldIsEmpty()
    {
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<DescopeException>(() =>
            client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(new SearchGovernanceAccessRequestsRequest
            {
                Sort = new List<SortField> { new() { Field = "createdTime" }, new() { Field = "" } },
            }));
        handler.Calls.Should().Be(0);
    }
}
