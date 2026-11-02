using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Descope;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Projectv1;
using Descope.Mgmt.Models.Searchv1;
using SortField = Descope.Mgmt.Models.Projectv1.SortField;
using FluentAssertions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Unit tests for the agentic management endpoints (agentic identities and the Agentic Governance Suite).
/// Requests go through the real Kiota HTTP adapter and authentication provider, with a capturing
/// HTTP handler in place of the network, so the method, route, Authorization header and body are checked as sent.
/// </summary>
public class AgenticTests
{
    private const string TestProjectId = "test_project_id";
    private const string TestManagementKey = "test-management-key";
    private const string BaseUrl = "https://api.descope.com";

    private const string GovernanceAccessRequestJson = @"{
        ""id"": ""req-1"",
        ""requestorUserId"": ""user-1"",
        ""requestorName"": ""Requestor"",
        ""clientType"": ""agent"",
        ""grantTypes"": [""clientCredentials""],
        ""targets"": [{""name"": ""res"", ""kind"": ""resource"", ""targetId"": ""target-1"", ""scopes"": [""read""], ""allScopes"": true}],
        ""scopes"": [""scope-1""],
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
        ""clientName"": ""My Agent"",
        ""clientDeleted"": true
    }";

    #region Agentic identities

    [Fact]
    public async Task SearchAgenticIdentities_SendsRequestAndParsesResponse()
    {
        var (client, handler) = CreateClient(@"{
            ""identities"": [{
                ""consent"": {""id"": ""consent-1"", ""appId"": ""app-1"", ""userId"": ""user-1"", ""scopes"": [""read""], ""grantedBy"": ""user-1"",
                              ""createdTime"": 10, ""tenantId"": ""tenant-1"", ""ExpireTime"": 20, ""modifiedTime"": 30, ""authMethod"": ""idJag""},
                ""agentName"": ""Agent"", ""resourceId"": ""res-1"", ""clientId"": ""client-1"", ""createdBy"": ""creator""
            }],
            ""total"": 1
        }");

        var response = await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(new SearchAgenticIdentitiesRequest
        {
            UserId = "user-1",
            ClientId = "client-1",
            ResourceId = "res-1",
            ConsentId = "consent-1",
            Page = 2,
            Limit = 5,
        });

        handler.AssertRequest(HttpMethod.Post, "/v1/mgmt/agentic/identities/search");
        var body = handler.BodyJson();
        body.GetProperty("userId").GetString().Should().Be("user-1");
        body.GetProperty("clientId").GetString().Should().Be("client-1");
        body.GetProperty("resourceId").GetString().Should().Be("res-1");
        body.GetProperty("consentId").GetString().Should().Be("consent-1");
        body.GetProperty("page").GetInt32().Should().Be(2);
        body.GetProperty("limit").GetInt32().Should().Be(5);

        response.Should().NotBeNull();
        response!.Total.Should().Be(1);
        response.Identities.Should().HaveCount(1);
        var identity = response.Identities![0];
        identity.AgentName.Should().Be("Agent");
        identity.ResourceId.Should().Be("res-1");
        identity.ClientId.Should().Be("client-1");
        identity.CreatedBy.Should().Be("creator");
        identity.Consent!.Id.Should().Be("consent-1");
        identity.Consent.AppId.Should().Be("app-1");
        identity.Consent.UserId.Should().Be("user-1");
        identity.Consent.Scopes.Should().Equal("read");
        identity.Consent.GrantedBy.Should().Be("user-1");
        identity.Consent.CreatedTime.Should().Be(10);
        identity.Consent.TenantId.Should().Be("tenant-1");
        identity.Consent.ExpireTime.Should().Be(20);
        identity.Consent.ModifiedTime.Should().Be(30);
        identity.Consent.AuthMethod.Should().Be("idJag");
    }

    [Fact]
    public async Task SearchAgenticIdentities_ThrowsWhenRequestIsNull()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Mgmt.V1.Agentic.Identities.Search.PostAsync(null!));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAgenticIdentities_SendsRequestAndParsesResponse()
    {
        var (client, handler) = CreateClient(@"{""revoked"": 3}");

        var response = await client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(new RevokeAgenticIdentitiesRequest
        {
            UserId = "user-1",
            ClientId = "client-1",
            ResourceId = "res-1",
            ConsentId = "consent-1",
        });

        handler.AssertRequest(HttpMethod.Post, "/v1/mgmt/agentic/identities/revoke");
        var body = handler.BodyJson();
        body.GetProperty("userId").GetString().Should().Be("user-1");
        body.GetProperty("clientId").GetString().Should().Be("client-1");
        body.GetProperty("resourceId").GetString().Should().Be("res-1");
        body.GetProperty("consentId").GetString().Should().Be("consent-1");

        response!.Revoked.Should().Be(3);
    }

    [Fact]
    public async Task RevokeAgenticIdentities_ThrowsWhenRequestIsNull()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(null!));
        handler.Request.Should().BeNull();
    }

    #endregion

    #region Governance settings

    [Fact]
    public async Task GetGovernanceSettings_SendsRequestAndParsesResponse()
    {
        var (client, handler) = CreateClient(@"{""configured"": true, ""autoApproval"": true, ""suiteDisabled"": true}");

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.GetAsync();

        handler.AssertRequest(HttpMethod.Get, "/v1/mgmt/agentic/governance/settings");
        handler.Request!.RequestUri!.Query.Should().BeEmpty();
        handler.Body.Should().BeNullOrEmpty();
        response!.Configured.Should().BeTrue();
        response.AutoApproval.Should().BeTrue();
        response.SuiteDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetGovernanceSettings_ParsesMissingOptionalFieldsAsNull()
    {
        var (client, _) = CreateClient("{}");

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.GetAsync();

        response!.Configured.Should().BeNull();
        response.AutoApproval.Should().BeNull();
        response.SuiteDisabled.Should().BeNull();
    }

    [Fact]
    public async Task SetGovernanceSettings_SendsRequestAndParsesResponse()
    {
        var (client, handler) = CreateClient(@"{""configured"": true, ""autoApproval"": false, ""suiteDisabled"": true}");

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.PostAsync(new AgenticGovernanceSettings
        {
            Configured = true,
            AutoApproval = false,
            SuiteDisabled = true,
        });

        handler.AssertRequest(HttpMethod.Post, "/v1/mgmt/agentic/governance/settings");
        var body = handler.BodyJson();
        body.GetProperty("configured").GetBoolean().Should().BeTrue();
        body.GetProperty("autoApproval").GetBoolean().Should().BeFalse();
        body.GetProperty("suiteDisabled").GetBoolean().Should().BeTrue();

        response!.Configured.Should().BeTrue();
        response.AutoApproval.Should().BeFalse();
        response.SuiteDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task SetGovernanceSettings_OmitsUnsetOptionalFields()
    {
        var (client, handler) = CreateClient("{}");

        await client.Mgmt.V1.Agentic.Governance.Settings.PostAsync(new AgenticGovernanceSettings { AutoApproval = true });

        var body = handler.BodyJson();
        body.GetProperty("autoApproval").GetBoolean().Should().BeTrue();
        body.TryGetProperty("configured", out _).Should().BeFalse();
        body.TryGetProperty("suiteDisabled", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SetGovernanceSettings_ThrowsWhenRequestIsNull()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Mgmt.V1.Agentic.Governance.Settings.PostAsync(null!));
        handler.Request.Should().BeNull();
    }

    #endregion

    #region Governance access requests

    [Fact]
    public async Task CreateGovernanceAccessRequest_SendsRequestAndParsesResponse()
    {
        var (client, handler) = CreateClient(GovernanceAccessRequestJson);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithValidationAsync(new CreateGovernanceAccessRequestRequest
        {
            RequestorUserId = "user-1",
            RequestorName = "Requestor",
            ClientName = "My Agent",
            ClientType = "agent",
            GrantTypes = new List<string> { "clientCredentials" },
            Targets = new List<GovernanceAccessRequestTarget>
            {
                new() { Name = "res", Kind = "resource", TargetId = "target-1", Scopes = new List<string> { "read" }, AllScopes = true },
            },
            Scopes = new List<string> { "scope-1" },
            Justification = "because",
        });

        handler.AssertRequest(HttpMethod.Post, "/v1/mgmt/agentic/governance/requests/create");
        var body = handler.BodyJson();
        body.GetProperty("requestorUserId").GetString().Should().Be("user-1");
        body.GetProperty("requestorName").GetString().Should().Be("Requestor");
        body.GetProperty("clientName").GetString().Should().Be("My Agent");
        body.GetProperty("clientType").GetString().Should().Be("agent");
        body.GetProperty("grantTypes")[0].GetString().Should().Be("clientCredentials");
        var target = body.GetProperty("targets")[0];
        target.GetProperty("name").GetString().Should().Be("res");
        target.GetProperty("kind").GetString().Should().Be("resource");
        target.GetProperty("targetId").GetString().Should().Be("target-1");
        target.GetProperty("scopes")[0].GetString().Should().Be("read");
        target.GetProperty("allScopes").GetBoolean().Should().BeTrue();
        body.GetProperty("scopes")[0].GetString().Should().Be("scope-1");
        body.GetProperty("justification").GetString().Should().Be("because");

        AssertGovernanceAccessRequest(response);
    }

    [Fact]
    public async Task CreateGovernanceAccessRequest_ThrowsWhenRequestIsNull()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithValidationAsync(null!));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task CreateGovernanceAccessRequest_ThrowsWhenRequestorUserIdIsMissing()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithValidationAsync(
            new CreateGovernanceAccessRequestRequest { ClientName = "My Agent" }));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task CreateGovernanceAccessRequest_ThrowsWhenGrantTypeIsEmpty()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithValidationAsync(
            new CreateGovernanceAccessRequestRequest { RequestorUserId = "user-1", GrantTypes = new List<string> { "authCode", "" } }));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task GetGovernanceAccessRequest_SendsRequestAndParsesResponse()
    {
        var (client, handler) = CreateClient(GovernanceAccessRequestJson);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.GetPath.PostWithIdAsync("req-1");

        handler.AssertRequest(HttpMethod.Post, "/v1/mgmt/agentic/governance/requests/get");
        handler.BodyJson().GetProperty("id").GetString().Should().Be("req-1");
        AssertGovernanceAccessRequest(response);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetGovernanceAccessRequest_ThrowsWhenIdIsMissing(string? id)
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.GetPath.PostWithIdAsync(id!));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task ResolveGovernanceAccessRequest_SendsRequestAndParsesResponse()
    {
        var (client, handler) = CreateClient(GovernanceAccessRequestJson);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithValidationAsync(new ResolveGovernanceAccessRequestRequest
        {
            Id = "req-1",
            Action = "approve",
            ResolutionNote = "ok",
            ResolvedByUserId = "admin-1",
            ResolvedByName = "Admin",
        });

        handler.AssertRequest(HttpMethod.Post, "/v1/mgmt/agentic/governance/requests/resolve");
        var body = handler.BodyJson();
        body.GetProperty("id").GetString().Should().Be("req-1");
        body.GetProperty("action").GetString().Should().Be("approve");
        body.GetProperty("resolutionNote").GetString().Should().Be("ok");
        body.GetProperty("resolvedByUserId").GetString().Should().Be("admin-1");
        body.GetProperty("resolvedByName").GetString().Should().Be("Admin");
        AssertGovernanceAccessRequest(response);
    }

    [Fact]
    public async Task ResolveGovernanceAccessRequest_ThrowsWhenRequestIsNull()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithValidationAsync(null!));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task ResolveGovernanceAccessRequest_ThrowsWhenIdIsMissing()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithValidationAsync(
            new ResolveGovernanceAccessRequestRequest { Action = "approve" }));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task ResolveGovernanceAccessRequest_ThrowsWhenActionIsMissing()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithValidationAsync(
            new ResolveGovernanceAccessRequestRequest { Id = "req-1" }));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task SearchGovernanceAccessRequests_SendsRequestAndParsesResponse()
    {
        var (client, handler) = CreateClient($@"{{""requests"": [{GovernanceAccessRequestJson}], ""total"": 1}}");

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(new SearchGovernanceAccessRequestsRequest
        {
            Limit = 10,
            Page = 1,
            SearchFields = new List<SearchField>
            {
                new() { Field = "status", ValStr = "pending", ValArr = new List<string> { "pending" }, ValType = "string", Negative = true },
            },
            Sort = new List<SortField> { new() { Field = "createdTime", Desc = true } },
            Text = "agent",
        });

        handler.AssertRequest(HttpMethod.Post, "/v1/mgmt/agentic/governance/requests/search");
        var body = handler.BodyJson();
        body.GetProperty("limit").GetInt32().Should().Be(10);
        body.GetProperty("page").GetInt32().Should().Be(1);
        var searchField = body.GetProperty("searchFields")[0];
        searchField.GetProperty("field").GetString().Should().Be("status");
        searchField.GetProperty("valStr").GetString().Should().Be("pending");
        searchField.GetProperty("valArr")[0].GetString().Should().Be("pending");
        searchField.GetProperty("valType").GetString().Should().Be("string");
        searchField.GetProperty("negative").GetBoolean().Should().BeTrue();
        var sort = body.GetProperty("sort")[0];
        sort.GetProperty("field").GetString().Should().Be("createdTime");
        sort.GetProperty("desc").GetBoolean().Should().BeTrue();
        body.GetProperty("text").GetString().Should().Be("agent");

        response!.Total.Should().Be(1);
        response.Requests.Should().HaveCount(1);
        AssertGovernanceAccessRequest(response.Requests![0]);
    }

    [Fact]
    public async Task SearchGovernanceAccessRequests_ThrowsWhenRequestIsNull()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(null!));
        handler.Request.Should().BeNull();
    }

    [Fact]
    public async Task SearchGovernanceAccessRequests_ThrowsWhenSortFieldIsEmpty()
    {
        var (client, handler) = CreateClient("{}");
        await Assert.ThrowsAsync<DescopeException>(() => client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(
            new SearchGovernanceAccessRequestsRequest { Sort = new List<SortField> { new() { Field = "createdTime" }, new() { Desc = true } } }));
        handler.Request.Should().BeNull();
    }

    #endregion

    #region Helpers

    private static void AssertGovernanceAccessRequest(GovernanceAccessRequest? response)
    {
        response.Should().NotBeNull();
        response!.Id.Should().Be("req-1");
        response.RequestorUserId.Should().Be("user-1");
        response.RequestorName.Should().Be("Requestor");
        response.ClientType.Should().Be("agent");
        response.GrantTypes.Should().Equal("clientCredentials");
        response.Targets.Should().HaveCount(1);
        response.Targets![0].Name.Should().Be("res");
        response.Targets[0].Kind.Should().Be("resource");
        response.Targets[0].TargetId.Should().Be("target-1");
        response.Targets[0].Scopes.Should().Equal("read");
        response.Targets[0].AllScopes.Should().BeTrue();
        response.Scopes.Should().Equal("scope-1");
        response.Justification.Should().Be("because");
        response.Status.Should().Be("approved");
        response.ResolvedByUserId.Should().Be("admin-1");
        response.ResolvedByName.Should().Be("Admin");
        response.ResolutionNote.Should().Be("ok");
        response.ClientId.Should().Be("client-1");
        response.ClientAppId.Should().Be("app-1");
        response.CreatedTime.Should().Be("1700000000");
        response.ModifiedTime.Should().Be("1700000001");
        response.Version.Should().Be("3");
        response.ClientName.Should().Be("My Agent");
        response.ClientDeleted.Should().BeTrue();
    }

    private static (IDescopeClient Client, CapturingHandler Handler) CreateClient(string responseJson)
    {
        var options = new DescopeClientOptions { ProjectId = TestProjectId, ManagementKey = TestManagementKey, BaseUrl = BaseUrl };
        var handler = new CapturingHandler(responseJson);
        var httpClient = new HttpClient(new DescopeErrorResponseHandler { InnerHandler = handler });
        var mgmtAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(TestProjectId, TestManagementKey), httpClient: httpClient)
        {
            BaseUrl = BaseUrl
        };
        var authAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(TestProjectId, null), httpClient: httpClient)
        {
            BaseUrl = BaseUrl
        };
        return (DescopeManagementClientFactory.CreateForTest(authAdapter, mgmtAdapter, options, httpClient), handler);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public CapturingHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        public void AssertRequest(HttpMethod method, string path)
        {
            Request.Should().NotBeNull();
            Request!.Method.Should().Be(method);
            Request.RequestUri!.AbsolutePath.Should().Be(path);
            Request.Headers.Authorization.Should().NotBeNull();
            Request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            Request.Headers.Authorization.Parameter.Should().Be($"{TestProjectId}:{TestManagementKey}");
        }

        public JsonElement BodyJson()
        {
            Body.Should().NotBeNullOrEmpty();
            using var document = JsonDocument.Parse(Body!);
            return document.RootElement.Clone();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    #endregion
}
