using System.Text.Json;
using Descope.Mgmt.Models.Managementv1;
using FluentAssertions;
using Xunit;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Tests for the agentic identities management API against a mock Descope backend.
/// They run only when DESCOPE_MOCK_URL is set, and return immediately otherwise.
/// </summary>
[Collection("DescopeMock")]
public class AgenticMockTests
{
    private const string TestProjectId = "test_project_id";
    private const string TestManagementKey = "test_management_key";
    private const string TestUserId = "U2abcdefghijklmnopqrstuvwxyz";

    private static string? MockUrl => Environment.GetEnvironmentVariable("DESCOPE_MOCK_URL");

    [Fact]
    public async Task SearchAgenticIdentities_AgainstMock()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient(MockUrl!);

        var response = await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(new SearchAgenticIdentitiesRequest
        {
            UserId = TestUserId,
            ClientId = "client1",
            ResourceId = "resource1",
            ConsentId = "consent1",
            Page = 1,
            Limit = 10,
        });

        using var last = await GetLastCall(MockUrl!);
        var root = last.RootElement;
        root.GetProperty("path").GetString().Should().Be("/v1/mgmt/agentic/identities/search");
        root.GetProperty("status").GetInt32().Should().Be(200);

        var request = root.GetProperty("request");
        request.GetProperty("userId").GetString().Should().Be(TestUserId);
        request.GetProperty("clientId").GetString().Should().Be("client1");
        request.GetProperty("resourceId").GetString().Should().Be("resource1");
        request.GetProperty("consentId").GetString().Should().Be("consent1");
        request.GetProperty("page").GetInt32().Should().Be(1);
        request.GetProperty("limit").GetInt32().Should().Be(10);

        var expected = root.GetProperty("responseBody");
        response.Should().NotBeNull();
        response!.Total.Should().Be(expected.GetProperty("total").GetInt32());
        var expectedIdentities = expected.GetProperty("identities");
        response.Identities.Should().HaveCount(expectedIdentities.GetArrayLength());
        var expectedIdentity = expectedIdentities[0];
        var identity = response.Identities![0];
        identity.AgentName.Should().Be(expectedIdentity.GetProperty("agentName").GetString());
        identity.ResourceId.Should().Be(expectedIdentity.GetProperty("resourceId").GetString());
        identity.ClientId.Should().Be(expectedIdentity.GetProperty("clientId").GetString());
        identity.CreatedBy.Should().Be(expectedIdentity.GetProperty("createdBy").GetString());

        var expectedConsent = expectedIdentity.GetProperty("consent");
        var consent = identity.Consent;
        consent.Should().NotBeNull();
        consent!.Id.Should().Be(expectedConsent.GetProperty("id").GetString());
        consent.AppId.Should().Be(expectedConsent.GetProperty("appId").GetString());
        consent.UserId.Should().Be(expectedConsent.GetProperty("userId").GetString());
        consent.Scopes.Should().Equal(expectedConsent.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()));
        consent.GrantedBy.Should().Be(expectedConsent.GetProperty("grantedBy").GetString());
        consent.CreatedTime.Should().Be(expectedConsent.GetProperty("createdTime").GetInt32());
        consent.TenantId.Should().Be(expectedConsent.GetProperty("tenantId").GetString());
        consent.ExpireTime.Should().Be(expectedConsent.GetProperty("ExpireTime").GetInt32());
        consent.ModifiedTime.Should().Be(expectedConsent.GetProperty("modifiedTime").GetInt32());
        consent.AuthMethod.Should().Be(expectedConsent.GetProperty("authMethod").GetString());
    }

    [Fact]
    public async Task RevokeAgenticIdentities_AgainstMock()
    {
        if (string.IsNullOrEmpty(MockUrl)) return;
        var client = CreateClient(MockUrl!);

        var response = await client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(new RevokeAgenticIdentitiesRequest
        {
            UserId = TestUserId,
            ClientId = "client1",
            ResourceId = "resource1",
            ConsentId = "consent1",
        });

        using var last = await GetLastCall(MockUrl!);
        var root = last.RootElement;
        root.GetProperty("path").GetString().Should().Be("/v1/mgmt/agentic/identities/revoke");
        root.GetProperty("status").GetInt32().Should().Be(200);

        var request = root.GetProperty("request");
        request.GetProperty("userId").GetString().Should().Be(TestUserId);
        request.GetProperty("clientId").GetString().Should().Be("client1");
        request.GetProperty("resourceId").GetString().Should().Be("resource1");
        request.GetProperty("consentId").GetString().Should().Be("consent1");

        var expected = root.GetProperty("responseBody");
        response.Should().NotBeNull();
        response!.Revoked.Should().Be(expected.GetProperty("revoked").GetInt32());
    }

    private static IDescopeClient CreateClient(string mockUrl)
    {
        return DescopeManagementClientFactory.Create(new DescopeClientOptions
        {
            ProjectId = TestProjectId,
            ManagementKey = TestManagementKey,
            BaseUrl = mockUrl,
        });
    }

    private static async Task<JsonDocument> GetLastCall(string mockUrl)
    {
        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync($"{mockUrl.TrimEnd('/')}/__mock/last");
        return JsonDocument.Parse(json);
    }
}
