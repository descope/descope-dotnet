using System.Collections;
using System.Net.Http;
using System.Text.Json;
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
/// Tests the agentic management endpoints against the Descope mock backend at DESCOPE_MOCK_URL.
/// The mock decodes and validates each request like the real backend and answers with every response field set.
/// Each test returns right away when DESCOPE_MOCK_URL is not set.
/// </summary>
[Collection("DescopeMock")]
public class AgenticMockTests
{
    private static readonly string? MockUrl = Environment.GetEnvironmentVariable("DESCOPE_MOCK_URL");

    [Fact]
    public async Task SearchAgenticIdentities()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient();
        var request = new SearchAgenticIdentitiesRequest
        {
            UserId = "U2abcdefghijklmnopqrstuvwxyz",
            ClientId = "client-1",
            ResourceId = "res-1",
            ConsentId = "consent-1",
            Page = 1,
            Limit = 10,
        };

        var response = await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(request);

        await AssertLastCall("POST", "/v1/mgmt/agentic/identities/search", request, response);
    }

    [Fact]
    public async Task RevokeAgenticIdentities()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient();
        var request = new RevokeAgenticIdentitiesRequest
        {
            UserId = "U2abcdefghijklmnopqrstuvwxyz",
            ClientId = "client-1",
            ResourceId = "res-1",
            ConsentId = "consent-1",
        };

        var response = await client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(request);

        await AssertLastCall("POST", "/v1/mgmt/agentic/identities/revoke", request, response);
    }

    [Fact]
    public async Task GetGovernanceSettings()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient();

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.GetAsync();

        await AssertLastCall("GET", "/v1/mgmt/agentic/governance/settings", null, response);
    }

    [Fact]
    public async Task SetGovernanceSettings()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient();
        var request = new AgenticGovernanceSettings
        {
            Configured = true,
            AutoApproval = true,
            SuiteDisabled = true,
        };

        var response = await client.Mgmt.V1.Agentic.Governance.Settings.PostAsync(request);

        await AssertLastCall("POST", "/v1/mgmt/agentic/governance/settings", request, response);
    }

    [Fact]
    public async Task CreateGovernanceAccessRequest()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient();
        var request = new CreateGovernanceAccessRequestRequest
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
        };

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Create.PostWithValidationAsync(request);

        await AssertLastCall("POST", "/v1/mgmt/agentic/governance/requests/create", request, response);
    }

    [Fact]
    public async Task GetGovernanceAccessRequest()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient();

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.GetPath.PostWithIdAsync("req-1");

        await AssertLastCall("POST", "/v1/mgmt/agentic/governance/requests/get",
            new GetGovernanceAccessRequestRequest { Id = "req-1" }, response);
    }

    [Fact]
    public async Task ResolveGovernanceAccessRequest()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient();
        var request = new ResolveGovernanceAccessRequestRequest
        {
            Id = "req-1",
            Action = "approve",
            ResolutionNote = "ok",
            ResolvedByUserId = "admin-1",
            ResolvedByName = "Admin",
        };

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Resolve.PostWithValidationAsync(request);

        await AssertLastCall("POST", "/v1/mgmt/agentic/governance/requests/resolve", request, response);
    }

    [Fact]
    public async Task SearchGovernanceAccessRequests()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient();
        var request = new SearchGovernanceAccessRequestsRequest
        {
            Limit = 10,
            Page = 1,
            SearchFields = new List<SearchField>
            {
                new() { Field = "status", ValStr = "pending", ValArr = new List<string> { "pending" }, ValType = "string", Negative = true },
            },
            Sort = new List<SortField> { new() { Field = "createdTime", Desc = true } },
            Text = "agent",
        };

        var response = await client.Mgmt.V1.Agentic.Governance.Requests.Search.PostWithValidationAsync(request);

        await AssertLastCall("POST", "/v1/mgmt/agentic/governance/requests/search", request, response);
    }

    private static IDescopeClient CreateClient()
    {
        return DescopeManagementClientFactory.Create(new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test-management-key",
            BaseUrl = MockUrl,
        });
    }

    /// <summary>
    /// Reads the mock's last call and checks the method and path, that the backend decoded every request
    /// field the test set, and that every response field the mock sent was parsed into a typed property.
    /// </summary>
    private static async Task AssertLastCall(string method, string path, IParsable? request, IParsable? response)
    {
        using var httpClient = new HttpClient();
        var lastJson = await httpClient.GetStringAsync($"{MockUrl!.TrimEnd('/')}/__mock/last");
        using var last = JsonDocument.Parse(lastJson);
        var root = last.RootElement;

        root.GetProperty("method").GetString().Should().Be(method);
        root.GetProperty("path").GetString().Should().Be(path);
        root.GetProperty("status").GetInt32().Should().Be(200, lastJson);

        if (request != null)
        {
            using var sent = JsonDocument.Parse(await KiotaJsonSerializer.SerializeAsStringAsync(request));
            AssertJsonContains(sent.RootElement, root.GetProperty("request"), "request");
        }

        response.Should().NotBeNull();
        AssertNoAdditionalData(response!, "response");
        using var parsed = JsonDocument.Parse(await KiotaJsonSerializer.SerializeAsStringAsync(response!));
        AssertJsonContains(root.GetProperty("responseBody"), parsed.RootElement, "response");
    }

    /// <summary>Asserts that every field of <paramref name="expected"/> is present with the same value in <paramref name="actual"/>.</summary>
    private static void AssertJsonContains(JsonElement expected, JsonElement actual, string path)
    {
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                actual.ValueKind.Should().Be(JsonValueKind.Object, path);
                foreach (var property in expected.EnumerateObject())
                {
                    actual.TryGetProperty(property.Name, out var actualValue).Should().BeTrue($"{path}.{property.Name} should be present");
                    AssertJsonContains(property.Value, actualValue, $"{path}.{property.Name}");
                }
                break;
            case JsonValueKind.Array:
                actual.ValueKind.Should().Be(JsonValueKind.Array, path);
                actual.GetArrayLength().Should().Be(expected.GetArrayLength(), path);
                var index = 0;
                foreach (var (expectedItem, actualItem) in expected.EnumerateArray().Zip(actual.EnumerateArray()))
                {
                    AssertJsonContains(expectedItem, actualItem, $"{path}[{index++}]");
                }
                break;
            default:
                actual.ValueKind.Should().Be(expected.ValueKind, path);
                actual.ToString().Should().Be(expected.ToString(), path);
                break;
        }
    }

    /// <summary>Asserts that no response field ended up in Kiota's untyped AdditionalData, at any depth.</summary>
    private static void AssertNoAdditionalData(object value, string path)
    {
        if (value is IAdditionalDataHolder holder)
        {
            holder.AdditionalData.Should().BeEmpty($"{path} should have no untyped fields");
        }
        foreach (var property in value.GetType().GetProperties().Where(p => p.Name != "AdditionalData" && p.GetIndexParameters().Length == 0))
        {
            var child = property.GetValue(value);
            if (child is IParsable)
            {
                AssertNoAdditionalData(child, $"{path}.{property.Name}");
            }
            else if (child is IEnumerable items and not string)
            {
                foreach (var item in items)
                {
                    if (item is IParsable)
                    {
                        AssertNoAdditionalData(item, $"{path}.{property.Name}[]");
                    }
                }
            }
        }
    }
}
