using Descope;
using Descope.Mgmt.Models;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Orchestrationv1;
using Descope.Mgmt.Models.Userv1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Xunit;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Unit tests for MgmtExtensions methods.
/// Validates that extension methods correctly transform and pass data.
/// This test focuses on verifying the field mapping logic between GetPasswordSettingsResponse
/// and ConfigurePasswordSettingsRequest to ensure all fields are copied correctly.
///
/// IMPORTANT: The PostWithSettingsResponseAsync_CopiesAllFieldsComprehensively test uses reflection
/// to automatically verify all fields are copied. If a new field is added to GetPasswordSettingsResponse
/// in the future (via Kiota regeneration), this test will fail unless:
/// 1. The field is also added to ConfigurePasswordSettingsRequest (auto-generated)
/// 2. The field is mapped in MgmtExtensions.PostWithSettingsResponseAsync
/// 3. The test's sourceSettings object is updated with a value for the new field
///
/// This ensures the test acts as a safeguard against incomplete field mappings.
/// </summary>
public class MgmtExtensionsTests
{
    /// <summary>
    /// Tests that LoadWithTenantIdAsync for Password Settings correctly passes the tenant ID.
    /// </summary>
    [Fact]
    public async Task PasswordSettings_LoadWithTenantIdAsync_PassesTenantIdCorrectly()
    {
        // Arrange
        var testTenantId = "test-tenant-id";

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<GetPasswordSettingsResponse>(
            requestInfo =>
            {
                // Assert that the tenant ID is passed correctly in the query parameters
                requestInfo.QueryParameters.Should().ContainKey("tenantId", "The tenant ID should be in query parameters");
                requestInfo.QueryParameters["tenantId"].Should().Be(testTenantId, "The tenant ID should match");
                return new GetPasswordSettingsResponse();
            });

        // Act
        await descopeClient.Mgmt.V1.Password.Settings.GetWithTenantIdAsync(testTenantId);
    }

    /// <summary>
    /// Tests that LoadWithTenantIdAsync for Password Settings throws when tenant ID is null.
    /// </summary>
    [Fact]
    public async Task PasswordSettings_LoadWithTenantIdAsync_ThrowsDescopeException_WhenTenantIdIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Password.Settings.GetWithTenantIdAsync(null!);
        });
    }

    /// <summary>
    /// Tests that LoadWithTenantIdAsync for Password Settings throws when tenant ID is empty.
    /// </summary>
    [Fact]
    public async Task PasswordSettings_LoadWithTenantIdAsync_ThrowsDescopeException_WhenTenantIdIsEmpty()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Password.Settings.GetWithTenantIdAsync("");
        });
    }

    /// <summary>
    /// Tests that PostWithSettingsResponseAsync throws ArgumentNullException when settings is null.
    /// </summary>
    [Fact]
    public async Task PostWithSettingsResponseAsync_ThrowsArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Password.Settings.PostWithSettingsResponseAsync(null!);
        });
    }

    /// <summary>
    /// Comprehensive test that verifies ALL fields are copied correctly in a single test.
    /// This test calls the actual extension method and uses reflection to ensure all properties
    /// from GetPasswordSettingsResponse can be mapped to ConfigurePasswordSettingsRequest.
    /// If a new field is added in the future, this test will fail unless the extension method
    /// is updated to map it.
    /// </summary>
    [Fact]
    public async Task PostWithSettingsResponseAsync_CopiesAllFieldsComprehensively()
    {
        // First, use reflection to ensure all properties exist in both types
        var responseProperties = typeof(GetPasswordSettingsResponse)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.Name != "AdditionalData") // Exclude internal Kiota property
            .ToList();

        var requestProperties = typeof(ConfigurePasswordSettingsRequest)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.Name != "AdditionalData") // Exclude internal Kiota property
            .ToDictionary(p => p.Name, p => p);

        // Assert that every property in the response has a matching property in the request
        foreach (var responseProp in responseProperties)
        {
            requestProperties.Should().ContainKey(responseProp.Name,
                $"ConfigurePasswordSettingsRequest should have a property '{responseProp.Name}' " +
                $"to match GetPasswordSettingsResponse.{responseProp.Name}");
        }

        Assert.True(responseProperties.Count > 0, "There should be at least one property to test");
        Assert.Equal(responseProperties.Count, requestProperties.Count);

        // Arrange - Create a response with ALL fields populated with distinct values
        var sourceSettings = new GetPasswordSettingsResponse
        {
            // Boolean fields - alternate true/false for easy verification
            Enabled = true,
            Lowercase = false,
            Uppercase = true,
            Number = false,
            NonAlphanumeric = true,
            EnablePasswordStrength = false,
            Expiration = true,
            Reuse = false,
            Lock = true,
            TempLock = false,

            // Integer fields - use distinct values
            MinLength = 8,
            PasswordStrengthScore = 3,
            ExpirationWeeks = 12,
            ReuseAmount = 5,
            LockAttempts = 3,
            TempLockAttempts = 5,
            TempLockDuration = 15,

            // String field
            TenantId = "test-tenant-xyz"
        };

        // Use CreateWithStreamAsserter to capture the actual request sent by the extension method
        ConfigurePasswordSettingsRequest? actualRequest = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<ConfigurePasswordSettingsRequest>(
            (requestInfo, request) =>
            {
                actualRequest = request;
            });

        // Act - Call the actual extension method
        await descopeClient.Mgmt.V1.Password.Settings.PostWithSettingsResponseAsync(sourceSettings);

        // Assert - Verify that the request was sent
        actualRequest.Should().NotBeNull("The extension method should have sent a request");

        // Use reflection to verify that ALL properties were correctly copied from source to the actual request
        foreach (var responseProp in responseProperties)
        {
            var requestProp = requestProperties[responseProp.Name];
            var sourceValue = responseProp.GetValue(sourceSettings);
            var actualValue = requestProp.GetValue(actualRequest!);

            actualValue.Should().Be(sourceValue,
                $"Property '{responseProp.Name}' must be mapped correctly in the extension method. " +
                $"If this fails, update MgmtExtensions.PostWithSettingsResponseAsync to include this property.");
        }

        // Explicit assertions for key fields to demonstrate the test is working
        actualRequest!.Enabled.Should().Be(true);
        actualRequest.MinLength.Should().Be(8);
        actualRequest.TenantId.Should().Be("test-tenant-xyz");
    }

    /// <summary>
    /// Tests that LoadUserAsync correctly passes the identifier to the request.
    /// </summary>
    [Fact]
    public async Task LoadUserAsync_PassesIdentifierCorrectly()
    {
        // Arrange
        var testIdentifier = "test-user@example.com";

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UserResponse>(
            requestInfo =>
            {
                // Assert that the identifier is passed correctly in the query parameters
                requestInfo.QueryParameters.Should().ContainKey("identifier", "The identifier should be in query parameters");
                requestInfo.QueryParameters["identifier"].Should().Be(testIdentifier, "The identifier should match");
                return new UserResponse();
            });

        // Act
        await descopeClient.Mgmt.V1.User.GetWithIdentifierAsync(testIdentifier);
    }

    /// <summary>
    /// Tests that LoadUserAsync throws DescopeException when identifier is null.
    /// </summary>
    [Fact]
    public async Task LoadUserAsync_ThrowsDescopeException_WhenIdentifierIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.User.GetWithIdentifierAsync(null!);
        });
    }

    /// <summary>
    /// Tests that LoadUserAsync throws DescopeException when identifier is empty.
    /// </summary>
    [Fact]
    public async Task LoadUserAsync_ThrowsDescopeException_WhenIdentifierIsEmpty()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.User.GetWithIdentifierAsync("");
        });
    }

    /// <summary>
    /// Tests that LoadAsync for AccessKey correctly passes the ID to the request.
    /// </summary>
    [Fact]
    public async Task AccessKey_LoadAsync_PassesIdCorrectly()
    {
        // Arrange
        var testId = "test-access-key-id";

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<AccessKeyResponse>(
            requestInfo =>
            {
                // Assert that the ID is passed correctly in the query parameters
                requestInfo.QueryParameters.Should().ContainKey("id", "The ID should be in query parameters");
                requestInfo.QueryParameters["id"].Should().Be(testId, "The ID should match");
                return new AccessKeyResponse();
            });

        // Act
        await descopeClient.Mgmt.V1.Accesskey.GetWithIdAsync(testId);
    }

    /// <summary>
    /// Tests that LoadAsync for AccessKey throws DescopeException when ID is null.
    /// </summary>
    [Fact]
    public async Task AccessKey_LoadAsync_ThrowsDescopeException_WhenIdIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Accesskey.GetWithIdAsync(null!);
        });
    }

    /// <summary>
    /// Tests that LoadAsync for AccessKey throws DescopeException when ID is empty.
    /// </summary>
    [Fact]
    public async Task AccessKey_LoadAsync_ThrowsDescopeException_WhenIdIsEmpty()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Accesskey.GetWithIdAsync("");
        });
    }

    /// <summary>
    /// Tests that LoadAsync for SSO Application correctly returns the application.
    /// </summary>
    [Fact]
    public async Task SsoApp_LoadAsync_PassesIdCorrectly()
    {
        // Arrange
        var testId = "test-sso-app-id";

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<LoadSSOApplicationResponse>(
            requestInfo =>
            {
                // Assert that the ID is passed correctly in the query parameters
                requestInfo.QueryParameters.Should().ContainKey("id", "The ID should be in query parameters");
                requestInfo.QueryParameters["id"].Should().Be(testId, "The ID should match");
                return new LoadSSOApplicationResponse();
            });

        // Act
        await descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync(testId);
    }

    /// <summary>
    /// Tests that LoadAsync for SSO Application throws when ID is null.
    /// </summary>
    [Fact]
    public async Task SsoApp_LoadAsync_ThrowsDescopeException_WhenIdIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync(null!);
        });
    }

    /// <summary>
    /// Tests that LoadAsync for SSO Application throws when ID is empty.
    /// </summary>
    [Fact]
    public async Task SsoApp_LoadAsync_ThrowsDescopeException_WhenIdIsEmpty()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync("");
        });
    }

    /// <summary>
    /// Tests that LoadAsync for SSO Settings correctly returns the settings.
    /// </summary>
    [Fact]
    public async Task SsoSettings_LoadAsync_PassesTenantIdCorrectly()
    {
        // Arrange
        var testTenantId = "test-tenant-id";

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<LoadSSOSettingsResponse>(
            requestInfo =>
            {
                // Assert that the tenant ID is passed correctly in the query parameters
                requestInfo.QueryParameters.Should().ContainKey("tenantId", "The tenant ID should be in query parameters");
                requestInfo.QueryParameters["tenantId"].Should().Be(testTenantId, "The tenant ID should match");
                return new LoadSSOSettingsResponse();
            });

        // Act
        await descopeClient.Mgmt.V2.Sso.Settings.GetWithTenantIdAsync(testTenantId);
    }

    /// <summary>
    /// Tests that LoadAsync for SSO Settings throws when tenant ID is null.
    /// </summary>
    [Fact]
    public async Task SsoSettings_LoadAsync_ThrowsDescopeException_WhenTenantIdIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V2.Sso.Settings.GetWithTenantIdAsync(null!);
        });
    }

    /// <summary>
    /// Tests that LoadAsync for SSO Settings throws when tenant ID is empty.
    /// </summary>
    [Fact]
    public async Task SsoSettings_LoadAsync_ThrowsDescopeException_WhenTenantIdIsEmpty()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V2.Sso.Settings.GetWithTenantIdAsync("");
        });
    }

    /// <summary>
    /// Tests that LoadAsync for Third Party Application correctly returns the application.
    /// </summary>
    [Fact]
    public async Task ThirdPartyApp_LoadAsync_PassesIdCorrectly()
    {
        // Arrange
        var testId = "test-app-id";

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<LoadThirdPartyApplicationResponse>(
            requestInfo =>
            {
                // Assert that the ID is passed correctly in the query parameters
                requestInfo.QueryParameters.Should().ContainKey("id", "The ID should be in query parameters");
                requestInfo.QueryParameters["id"].Should().Be(testId, "The ID should match");
                return new LoadThirdPartyApplicationResponse();
            });

        // Act
        await descopeClient.Mgmt.V1.Thirdparty.App.Load.GetWithIdAsync(testId);
    }

    /// <summary>
    /// Tests that LoadAsync for Third Party Application throws when ID is null.
    /// </summary>
    [Fact]
    public async Task ThirdPartyApp_LoadAsync_ThrowsDescopeException_WhenIdIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Thirdparty.App.Load.GetWithIdAsync(null!);
        });
    }

    /// <summary>
    /// Tests that LoadAsync for Third Party Application throws when ID is empty.
    /// </summary>
    [Fact]
    public async Task ThirdPartyApp_LoadAsync_ThrowsDescopeException_WhenIdIsEmpty()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Thirdparty.App.Load.GetWithIdAsync("");
        });
    }

    /// <summary>
    /// Tests that LoadWithClientIdAsync for Third Party Application correctly returns the application.
    /// </summary>
    [Fact]
    public async Task ThirdPartyApp_LoadWithClientIdAsync_PassesClientIdCorrectly()
    {
        // Arrange
        var testClientId = "test-client-id";

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<LoadThirdPartyApplicationResponse>(
            requestInfo =>
            {
                // Assert that the client ID is passed correctly in the query parameters
                requestInfo.QueryParameters.Should().ContainKey("clientId", "The client ID should be in query parameters");
                requestInfo.QueryParameters["clientId"].Should().Be(testClientId, "The client ID should match");
                return new LoadThirdPartyApplicationResponse();
            });

        // Act
        await descopeClient.Mgmt.V1.Thirdparty.App.Load.GetWithClientIdAsync(testClientId);
    }

    /// <summary>
    /// Tests that LoadWithClientIdAsync for Third Party Application throws when client ID is null.
    /// </summary>
    [Fact]
    public async Task ThirdPartyApp_LoadWithClientIdAsync_ThrowsDescopeException_WhenClientIdIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Thirdparty.App.Load.GetWithClientIdAsync(null!);
        });
    }

    /// <summary>
    /// Tests that LoadWithClientIdAsync for Third Party Application throws when client ID is empty.
    /// </summary>
    [Fact]
    public async Task ThirdPartyApp_LoadWithClientIdAsync_ThrowsDescopeException_WhenClientIdIsEmpty()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Thirdparty.App.Load.GetWithClientIdAsync("");
        });
    }

    /// <summary>
    /// Tests that DeleteWithTenantIdAsync for SSO Settings correctly passes the tenant ID.
    /// </summary>
    [Fact]
    public async Task SsoSettings_DeleteWithTenantIdAsync_PassesTenantIdCorrectly()
    {
        // Arrange
        var testTenantId = "test-tenant-id";

        var descopeClient = TestDescopeClientFactory.CreateWithStreamAsserter(requestInfo =>
        {
            // Assert that the tenant ID is passed correctly in the query parameters
            requestInfo.QueryParameters.Should().ContainKey("tenantId", "The tenant ID should be in query parameters");
            requestInfo.QueryParameters["tenantId"].Should().Be(testTenantId, "The tenant ID should match");
        });

        // Act
        await descopeClient.Mgmt.V1.Sso.Settings.DeleteWithTenantIdAsync(testTenantId);

        // No explicit assertion needed - the asserter will throw if parameters are incorrect
    }

    /// <summary>
    /// Tests that DeleteWithTenantIdAsync for SSO Settings throws when tenant ID is null.
    /// </summary>
    [Fact]
    public async Task SsoSettings_DeleteWithTenantIdAsync_ThrowsDescopeException_WhenTenantIdIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Sso.Settings.DeleteWithTenantIdAsync(null!);
        });
    }

    /// <summary>
    /// Tests that DeleteWithTenantIdAsync for SSO Settings throws when tenant ID is empty.
    /// </summary>
    [Fact]
    public async Task SsoSettings_DeleteWithTenantIdAsync_ThrowsDescopeException_WhenTenantIdIsEmpty()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Sso.Settings.DeleteWithTenantIdAsync("");
        });
    }

    /// <summary>
    /// Tests that PostWithJsonOutputAsync correctly deserializes flow output to JSON.
    /// </summary>
    [Fact]
    public async Task Flow_PostWithJsonOutputAsync_DeserializesOutputToJson()
    {
        // Arrange
        var response = new RunManagementFlowResponse
        {
            Output = new RunManagementFlowResponse_output
            {
                AdditionalData = new Dictionary<string, object>
                {
                    { "email", new UntypedString("test@example.com") },
                    { "count", new UntypedInteger(42) },
                    { "enabled", new UntypedBoolean(true) },
                    {
                        "obj",
                        new UntypedObject(new Dictionary<string, UntypedNode>
                        {
                            { "greeting", new UntypedString("Hello, World!") },
                            { "count", new UntypedInteger(100) }
                        })
                    }
                }
            }
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<RunManagementFlowResponse>(
            requestInfo => response);

        var request = new RunManagementFlowRequest
        {
            FlowId = "test-flow",
            Options = new ManagementFlowOptions
            {
                Input = new ManagementFlowOptions_input
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "email", "test@example.com" }
                    }
                }
            }
        };

        // Act
        var result = await descopeClient.Mgmt.V1.Flow.Run.PostWithJsonOutputAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Response.Should().NotBeNull();
        result.OutputJson.Should().NotBeNull();

        // Verify JSON properties
        var root = result.OutputJson!.RootElement;
        root.GetProperty("email").GetString().Should().Be("test@example.com");
        root.GetProperty("count").GetInt32().Should().Be(42);
        root.GetProperty("enabled").GetBoolean().Should().BeTrue();

        // Verify nested object
        var obj = root.GetProperty("obj");
        obj.GetProperty("greeting").GetString().Should().Be("Hello, World!");
        obj.GetProperty("count").GetInt32().Should().Be(100);
    }

    /// <summary>
    /// Tests that PostWithJsonOutputAsync throws DescopeException when request is null.
    /// </summary>
    [Fact]
    public async Task Flow_PostWithJsonOutputAsync_ThrowsDescopeException_WhenRequestIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await descopeClient.Mgmt.V1.Flow.Run.PostWithJsonOutputAsync(null!);
        });
    }

    /// <summary>
    /// Tests that PostWithJsonOutputAsync handles null output gracefully.
    /// </summary>
    [Fact]
    public async Task Flow_PostWithJsonOutputAsync_HandlesNullOutput()
    {
        // Arrange
        var response = new RunManagementFlowResponse
        {
            Output = null
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<RunManagementFlowResponse>(
            requestInfo => response);

        var request = new RunManagementFlowRequest
        {
            FlowId = "test-flow"
        };

        // Act
        var result = await descopeClient.Mgmt.V1.Flow.Run.PostWithJsonOutputAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Response.Should().NotBeNull();
        result.OutputJson.Should().BeNull();
    }

    /// <summary>
    /// Tests that PostWithJsonOutputAsync handles empty AdditionalData.
    /// </summary>
    [Fact]
    public async Task Flow_PostWithJsonOutputAsync_HandlesEmptyAdditionalData()
    {
        // Arrange
        var response = new RunManagementFlowResponse
        {
            Output = new RunManagementFlowResponse_output
            {
                AdditionalData = new Dictionary<string, object>()
            }
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<RunManagementFlowResponse>(
            requestInfo => response);

        var request = new RunManagementFlowRequest
        {
            FlowId = "test-flow"
        };

        // Act
        var result = await descopeClient.Mgmt.V1.Flow.Run.PostWithJsonOutputAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Response.Should().NotBeNull();
        result.OutputJson.Should().BeNull();
    }

    /// <summary>
    /// Tests that PostWithJsonOutputAsync correctly handles arrays in output.
    /// </summary>
    [Fact]
    public async Task Flow_PostWithJsonOutputAsync_HandlesArrays()
    {
        // Arrange
        var response = new RunManagementFlowResponse
        {
            Output = new RunManagementFlowResponse_output
            {
                AdditionalData = new Dictionary<string, object>
                {
                    {
                        "items",
                        new UntypedArray(new List<UntypedNode>
                        {
                            new UntypedString("item1"),
                            new UntypedString("item2"),
                            new UntypedString("item3")
                        })
                    }
                }
            }
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<RunManagementFlowResponse>(
            requestInfo => response);

        var request = new RunManagementFlowRequest
        {
            FlowId = "test-flow"
        };

        // Act
        var result = await descopeClient.Mgmt.V1.Flow.Run.PostWithJsonOutputAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.OutputJson.Should().NotBeNull();

        var items = result.OutputJson!.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(3);
        items[0].GetString().Should().Be("item1");
        items[1].GetString().Should().Be("item2");
        items[2].GetString().Should().Be("item3");
    }

}
