using System.Text;
using System.Text.Json;
using Descope.Mgmt.Models.Managementv1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Serialization.Json;
using Xunit;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Unit tests for the SAML accepted audiences fields on the SSO configure requests and the SSO settings response.
/// The request tests read the JSON body that is actually sent, so they can tell an omitted field from an empty list.
/// </summary>
public class SsoSamlAcceptedAudiencesTests
{
    [Fact]
    public async Task SsoSaml_Configure_SendsAcceptedAudiences()
    {
        // Arrange
        JsonElement? sentSettings = null;
        var descopeClient = TestDescopeClientFactory.CreateWithStreamAsserter(requestInfo =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/mgmt/sso/saml");
            sentSettings = ReadSettings(requestInfo);
        });

        // Act
        await descopeClient.Mgmt.V1.Sso.Saml.PostAsync(new ConfigureSSOSAMLSettingsRequest
        {
            TenantId = "tenant-id",
            Settings = new SSOSAMLSettings
            {
                IdpUrl = "https://idp.example.com/sso",
                EntityId = "https://idp.example.com/entity",
                IdpCert = "cert",
                AcceptedAudiences = new List<string> { "https://old-provider.example.com", "" },
                ReplaceAcceptedAudiences = true,
            },
        });

        // Assert
        sentSettings.Should().NotBeNull();
        ReadStrings(sentSettings!.Value, "acceptedAudiences").Should().Equal("https://old-provider.example.com", "");
        sentSettings.Value.GetProperty("replaceAcceptedAudiences").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SsoSaml_Configure_OmitsAcceptedAudiencesWhenNotSet()
    {
        // Arrange
        JsonElement? sentSettings = null;
        var descopeClient = TestDescopeClientFactory.CreateWithStreamAsserter(requestInfo =>
        {
            sentSettings = ReadSettings(requestInfo);
        });

        // Act
        await descopeClient.Mgmt.V1.Sso.Saml.PostAsync(new ConfigureSSOSAMLSettingsRequest
        {
            TenantId = "tenant-id",
            Settings = new SSOSAMLSettings
            {
                IdpUrl = "https://idp.example.com/sso",
                EntityId = "https://idp.example.com/entity",
                IdpCert = "cert",
            },
        });

        // Assert - leaving both fields unset keeps the stored accepted audiences
        sentSettings.Should().NotBeNull();
        sentSettings!.Value.TryGetProperty("acceptedAudiences", out _).Should().BeFalse();
        sentSettings.Value.TryGetProperty("replaceAcceptedAudiences", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SsoSaml_Configure_SendsEmptyAcceptedAudiencesToClear()
    {
        // Arrange
        JsonElement? sentSettings = null;
        var descopeClient = TestDescopeClientFactory.CreateWithStreamAsserter(requestInfo =>
        {
            sentSettings = ReadSettings(requestInfo);
        });

        // Act
        await descopeClient.Mgmt.V1.Sso.Saml.PostAsync(new ConfigureSSOSAMLSettingsRequest
        {
            TenantId = "tenant-id",
            Settings = new SSOSAMLSettings
            {
                IdpUrl = "https://idp.example.com/sso",
                EntityId = "https://idp.example.com/entity",
                IdpCert = "cert",
                AcceptedAudiences = new List<string>(),
                ReplaceAcceptedAudiences = true,
            },
        });

        // Assert - an empty list is sent as [] so that, with replaceAcceptedAudiences, it clears the stored values
        sentSettings.Should().NotBeNull();
        sentSettings!.Value.GetProperty("acceptedAudiences").ValueKind.Should().Be(JsonValueKind.Array);
        ReadStrings(sentSettings.Value, "acceptedAudiences").Should().BeEmpty();
        sentSettings.Value.GetProperty("replaceAcceptedAudiences").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SsoSamlByMetadata_Configure_SendsAcceptedAudiences()
    {
        // Arrange
        JsonElement? sentSettings = null;
        var descopeClient = TestDescopeClientFactory.CreateWithStreamAsserter(requestInfo =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/mgmt/sso/saml/metadata");
            sentSettings = ReadSettings(requestInfo);
        });

        // Act
        await descopeClient.Mgmt.V1.Sso.Saml.Metadata.PostAsync(new ConfigureSSOSAMLSettingsByMetadataRequest
        {
            TenantId = "tenant-id",
            Settings = new SSOSAMLByMetadataSettings
            {
                IdpMetadataUrl = "https://idp.example.com/metadata",
                AcceptedAudiences = new List<string> { "https://old-provider.example.com" },
                ReplaceAcceptedAudiences = true,
            },
        });

        // Assert
        sentSettings.Should().NotBeNull();
        ReadStrings(sentSettings!.Value, "acceptedAudiences").Should().Equal("https://old-provider.example.com");
        sentSettings.Value.GetProperty("replaceAcceptedAudiences").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SsoSamlByMetadata_Configure_OmitsAcceptedAudiencesWhenNotSet()
    {
        // Arrange
        JsonElement? sentSettings = null;
        var descopeClient = TestDescopeClientFactory.CreateWithStreamAsserter(requestInfo =>
        {
            sentSettings = ReadSettings(requestInfo);
        });

        // Act
        await descopeClient.Mgmt.V1.Sso.Saml.Metadata.PostAsync(new ConfigureSSOSAMLSettingsByMetadataRequest
        {
            TenantId = "tenant-id",
            Settings = new SSOSAMLByMetadataSettings
            {
                IdpMetadataUrl = "https://idp.example.com/metadata",
            },
        });

        // Assert
        sentSettings.Should().NotBeNull();
        sentSettings!.Value.TryGetProperty("acceptedAudiences", out _).Should().BeFalse();
        sentSettings.Value.TryGetProperty("replaceAcceptedAudiences", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SsoSamlByMetadata_Configure_SendsEmptyAcceptedAudiencesToClear()
    {
        // Arrange
        JsonElement? sentSettings = null;
        var descopeClient = TestDescopeClientFactory.CreateWithStreamAsserter(requestInfo =>
        {
            sentSettings = ReadSettings(requestInfo);
        });

        // Act
        await descopeClient.Mgmt.V1.Sso.Saml.Metadata.PostAsync(new ConfigureSSOSAMLSettingsByMetadataRequest
        {
            TenantId = "tenant-id",
            Settings = new SSOSAMLByMetadataSettings
            {
                IdpMetadataUrl = "https://idp.example.com/metadata",
                AcceptedAudiences = new List<string>(),
                ReplaceAcceptedAudiences = true,
            },
        });

        // Assert
        sentSettings.Should().NotBeNull();
        sentSettings!.Value.GetProperty("acceptedAudiences").ValueKind.Should().Be(JsonValueKind.Array);
        ReadStrings(sentSettings.Value, "acceptedAudiences").Should().BeEmpty();
        sentSettings.Value.GetProperty("replaceAcceptedAudiences").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SsoSettings_Load_DeserializesAcceptedAudiences()
    {
        // Arrange - the JSON as the API returns it
        const string json = "{\"tenant\":{\"id\":\"tenant-id\"},\"saml\":{\"idpEntityId\":\"https://idp.example.com/entity\","
            + "\"acceptedAudiences\":[\"https://old-provider.example.com\",\"\"]}}";

        // Act
        var response = await ParseAsync(json);

        // Assert
        response.Should().NotBeNull();
        response!.Saml.Should().NotBeNull();
        response.Saml!.AcceptedAudiences.Should().Equal("https://old-provider.example.com", "");
    }

    [Fact]
    public async Task SsoSettings_Load_LeavesAcceptedAudiencesNullWhenAbsent()
    {
        // Arrange
        const string json = "{\"tenant\":{\"id\":\"tenant-id\"},\"saml\":{\"idpEntityId\":\"https://idp.example.com/entity\"}}";

        // Act
        var response = await ParseAsync(json);

        // Assert
        response.Should().NotBeNull();
        response!.Saml.Should().NotBeNull();
        response.Saml!.AcceptedAudiences.Should().BeNull();
    }

    private static JsonElement ReadSettings(RequestInformation requestInfo)
    {
        requestInfo.Content.Should().NotBeNull();
        using var reader = new StreamReader(requestInfo.Content, Encoding.UTF8);
        using var document = JsonDocument.Parse(reader.ReadToEnd());
        return document.RootElement.GetProperty("settings").Clone();
    }

    private static List<string?> ReadStrings(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).EnumerateArray().Select(item => item.GetString()).ToList();
    }

    private static async Task<LoadSSOSettingsResponse?> ParseAsync(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var parseNode = await new JsonParseNodeFactory().GetRootParseNodeAsync("application/json", stream);
        return parseNode.GetObjectValue(LoadSSOSettingsResponse.CreateFromDiscriminatorValue);
    }
}
