using System.Text.Json;
using Descope.Mgmt.Models.Managementv1;
using FluentAssertions;
using Xunit;

namespace Descope.Test.UnitTests;

/// <summary>
/// Tests for the agentic management API against the mock backend at DESCOPE_MOCK_URL.
/// Each test returns right away when DESCOPE_MOCK_URL is unset.
/// </summary>
public class AgenticMockTests
{
    private const string TestUserId = "U2abcdefghijklmnopqrstuvwxyz";

    private static string? MockUrl => Environment.GetEnvironmentVariable("DESCOPE_MOCK_URL");

    private static IDescopeClient CreateClient(string mockUrl)
    {
        return DescopeManagementClientFactory.Create(new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key",
            BaseUrl = mockUrl.TrimEnd('/'),
        });
    }

    private static async Task<JsonElement> GetLastCallAsync(string mockUrl)
    {
        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync($"{mockUrl.TrimEnd('/')}/__mock/last");
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact]
    public async Task AgenticIdentities_Search_Mock()
    {
        var mockUrl = MockUrl;
        if (string.IsNullOrEmpty(mockUrl)) return;
        var client = CreateClient(mockUrl!);

        var response = await client.Mgmt.V1.Agentic.Identities.Search.PostAsync(new SearchAgenticIdentitiesRequest
        {
            UserId = TestUserId,
            ClientId = "client1",
            ResourceId = "resource1",
            ConsentId = "consent1",
            Page = 1,
            Limit = 10,
        });

        var last = await GetLastCallAsync(mockUrl!);
        last.GetProperty("path").GetString().Should().Be("/v1/mgmt/agentic/identities/search");
        var request = last.GetProperty("request");
        request.GetProperty("userId").GetString().Should().Be(TestUserId);
        request.GetProperty("clientId").GetString().Should().Be("client1");
        request.GetProperty("resourceId").GetString().Should().Be("resource1");
        request.GetProperty("consentId").GetString().Should().Be("consent1");
        request.GetProperty("page").GetInt32().Should().Be(1);
        request.GetProperty("limit").GetInt32().Should().Be(10);

        var expected = last.GetProperty("responseBody");
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
        identity.Consent.Should().NotBeNull();
        identity.Consent!.Id.Should().Be(expectedConsent.GetProperty("id").GetString());
        identity.Consent.AppId.Should().Be(expectedConsent.GetProperty("appId").GetString());
        identity.Consent.UserId.Should().Be(expectedConsent.GetProperty("userId").GetString());
        identity.Consent.Scopes.Should().BeEquivalentTo(expectedConsent.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()));
        identity.Consent.GrantedBy.Should().Be(expectedConsent.GetProperty("grantedBy").GetString());
        identity.Consent.CreatedTime.Should().Be(expectedConsent.GetProperty("createdTime").GetInt32());
        identity.Consent.TenantId.Should().Be(expectedConsent.GetProperty("tenantId").GetString());
        identity.Consent.ExpireTime.Should().Be(expectedConsent.GetProperty("ExpireTime").GetInt32());
        identity.Consent.ModifiedTime.Should().Be(expectedConsent.GetProperty("modifiedTime").GetInt32());
    }

    [Fact]
    public async Task AgenticIdentities_Revoke_Mock()
    {
        var mockUrl = MockUrl;
        if (string.IsNullOrEmpty(mockUrl)) return;
        var client = CreateClient(mockUrl!);

        var response = await client.Mgmt.V1.Agentic.Identities.Revoke.PostAsync(new RevokeAgenticIdentitiesRequest
        {
            UserId = TestUserId,
            ClientId = "client1",
            ResourceId = "resource1",
            ConsentId = "consent1",
        });

        var last = await GetLastCallAsync(mockUrl!);
        last.GetProperty("path").GetString().Should().Be("/v1/mgmt/agentic/identities/revoke");
        var request = last.GetProperty("request");
        request.GetProperty("userId").GetString().Should().Be(TestUserId);
        request.GetProperty("clientId").GetString().Should().Be("client1");
        request.GetProperty("resourceId").GetString().Should().Be("resource1");
        request.GetProperty("consentId").GetString().Should().Be("consent1");

        var expected = last.GetProperty("responseBody");
        response.Should().NotBeNull();
        response!.Revoked.Should().Be(expected.GetProperty("revoked").GetInt32());
    }
}
