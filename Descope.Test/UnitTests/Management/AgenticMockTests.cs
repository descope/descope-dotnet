using System.Text.Json;
using System.Text.Json.Nodes;
using Descope;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Projectv1;
using Descope.Mgmt.Models.Searchv1;
using FluentAssertions;
using Microsoft.Kiota.Abstractions.Serialization;
using Xunit;
using SortField = Descope.Mgmt.Models.Projectv1.SortField;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Tests for the agentic management APIs against the Descope mock backend at DESCOPE_MOCK_URL.
/// The mock runs the backend's real HTTP gateway and validation, and answers with every response field set.
/// Each test returns right away when DESCOPE_MOCK_URL is unset.
/// </summary>
[Collection("DescopeMock")]
public class AgenticMockTests
{
    private const string ProjectId = "test_project_id";
    private const string ManagementKey = "test_management_key";
    private const string UserId = "U2abcdefghijklmnopqrstuvwxyz";

    private static string? MockUrl => Environment.GetEnvironmentVariable("DESCOPE_MOCK_URL") is { Length: > 0 } url ? url.TrimEnd('/') : null;

    private static IDescopeClient CreateClient(string mockUrl) =>
        DescopeManagementClientFactory.Create(new DescopeClientOptions
        {
            ProjectId = ProjectId,
            ManagementKey = ManagementKey,
            BaseUrl = mockUrl,
        });

    private static async Task<JsonObject> GetLastCallAsync(string mockUrl)
    {
        using var http = new HttpClient();
        var json = await http.GetStringAsync($"{mockUrl}/__mock/last");
        return JsonNode.Parse(json)!.AsObject();
    }

    private static async Task<JsonNode?> SerializeAsync(IParsable value) =>
        JsonNode.Parse(await KiotaJsonSerializer.SerializeAsStringAsync(value));

    /// <summary>
    /// Asserts that every field of <paramref name="expected"/> is present in <paramref name="actual"/> with the same value.
    /// </summary>
    private static void AssertContains(JsonNode? expected, JsonNode? actual, string path = "$")
    {
        switch (expected)
        {
            case null:
                return;
            case JsonObject expectedObject:
                actual.Should().BeOfType<JsonObject>($"{path} should be an object");
                foreach (var (key, value) in expectedObject)
                {
                    actual!.AsObject().ContainsKey(key).Should().BeTrue($"{path}.{key} should be present");
                    AssertContains(value, actual[key], $"{path}.{key}");
                }
                return;
            case JsonArray expectedArray:
                actual.Should().BeOfType<JsonArray>($"{path} should be an array");
                actual!.AsArray().Count.Should().Be(expectedArray.Count, $"{path} should have the same length");
                for (var i = 0; i < expectedArray.Count; i++)
                {
                    AssertContains(expectedArray[i], actual[i], $"{path}[{i}]");
                }
                return;
            default:
                actual.Should().NotBeNull($"{path} should be set");
                actual!.ToJsonString().Should().Be(expected.ToJsonString(), $"{path} should match");
                return;
        }
    }

    private static async Task AssertLastCallAsync(string mockUrl, string method, string path, JsonNode? expectedRequest, IParsable? response)
    {
        var last = await GetLastCallAsync(mockUrl);
        last["method"]!.GetValue<string>().Should().Be(method);
        last["path"]!.GetValue<string>().Should().Be(path);
        last["status"]!.GetValue<int>().Should().Be(200);
        AssertContains(expectedRequest, last["request"], "request");
        response.Should().NotBeNull();
        AssertContains(last["responseBody"], await SerializeAsync(response!), "response");
    }

    [Fact]
    public async Task SearchAgenticIdentities_Mock()
    {
        if (MockUrl is not { } mockUrl) return;
        var client = CreateClient(mockUrl);
        var request = new SearchAgenticIdentitiesRequest
        {
            UserId = UserId,
            ClientId = "client-1",
            ResourceId = "res-1",
            ConsentId = "consent-1",
            Page = 1,
            Limit = 5,
        };

        var response = await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(request);

        await AssertLastCallAsync(mockUrl, "POST", "/v1/mgmt/agentic/identities/search", await SerializeAsync(request), response);
    }

    [Fact]
    public async Task RevokeAgenticIdentities_Mock()
    {
        if (MockUrl is not { } mockUrl) return;
        var client = CreateClient(mockUrl);
        var request = new RevokeAgenticIdentitiesRequest
        {
            UserId = UserId,
            ClientId = "client-1",
            ResourceId = "res-1",
            ConsentId = "consent-1",
        };

        var response = await client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(request);

        await AssertLastCallAsync(mockUrl, "POST", "/v1/mgmt/agentic/identities/revoke", await SerializeAsync(request), response);
    }

    [Fact]
    public async Task GetGovernanceSettings_Mock()
    {
        if (MockUrl is not { } mockUrl) return;
        var client = CreateClient(mockUrl);

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.GetAsync();

        await AssertLastCallAsync(mockUrl, "GET", "/v1/mgmt/agentic/governance/settings", null, response);
    }

    [Fact]
    public async Task SetGovernanceSettings_Mock()
    {
        if (MockUrl is not { } mockUrl) return;
        var client = CreateClient(mockUrl);
        var request = new AgenticGovernanceSettings
        {
            Configured = true,
            AutoApproval = true,
            SuiteDisabled = true,
        };

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.PostAsync(request);

        await AssertLastCallAsync(mockUrl, "POST", "/v1/mgmt/agentic/governance/settings", await SerializeAsync(request), response);
    }

    [Fact]
    public async Task CreateGovernanceAccessRequest_Mock()
    {
        if (MockUrl is not { } mockUrl) return;
        var client = CreateClient(mockUrl);
        var request = new CreateGovernanceAccessRequestRequest
        {
            RequestorName = "Requestor",
            ClientName = "My agent",
            ClientType = "agent",
            GrantTypes = new List<string> { "clientCredentials" },
            Targets = new List<GovernanceAccessRequestTarget>
            {
                new() { Name = "mcp", Kind = "resource", TargetId = "res-1", Scopes = new List<string> { "read" }, AllScopes = true },
            },
            Scopes = new List<string> { "openid" },
            Justification = "because",
        };

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithRequestorUserIdAsync(UserId, request);

        request.RequestorUserId.Should().Be(UserId);
        await AssertLastCallAsync(mockUrl, "POST", "/v1/mgmt/agentic/governance/requests/create", await SerializeAsync(request), response);
    }

    [Fact]
    public async Task GetGovernanceAccessRequest_Mock()
    {
        if (MockUrl is not { } mockUrl) return;
        var client = CreateClient(mockUrl);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.GetPath.PostWithIdAsync("req-1");

        await AssertLastCallAsync(mockUrl, "POST", "/v1/mgmt/agentic/governance/requests/get",
            JsonNode.Parse(@"{""id"": ""req-1""}"), response);
    }

    [Fact]
    public async Task ResolveGovernanceAccessRequest_Mock()
    {
        if (MockUrl is not { } mockUrl) return;
        var client = CreateClient(mockUrl);

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithIdAsync("req-1", "approve", "ok");

        await AssertLastCallAsync(mockUrl, "POST", "/v1/mgmt/agentic/governance/requests/resolve",
            JsonNode.Parse(@"{""id"": ""req-1"", ""action"": ""approve"", ""resolutionNote"": ""ok""}"), response);
    }

    [Fact]
    public async Task SearchGovernanceAccessRequests_Mock()
    {
        if (MockUrl is not { } mockUrl) return;
        var client = CreateClient(mockUrl);
        var request = new SearchGovernanceAccessRequestsRequest
        {
            Limit = 10,
            Page = 1,
            SearchFields = new List<SearchField>
            {
                new() { Field = "status", ValArr = new List<string> { "pending" }, ValStr = "pending", Negative = true, ValType = "string" },
            },
            Sort = new List<SortField> { new() { Field = "createdTime", Desc = true } },
            Text = "agent",
        };

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(request);

        await AssertLastCallAsync(mockUrl, "POST", "/v1/mgmt/agentic/governance/requests/search", await SerializeAsync(request), response);
    }
}
