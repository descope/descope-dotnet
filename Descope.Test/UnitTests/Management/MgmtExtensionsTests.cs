using Descope;
using Descope.Mgmt.Models;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Userv1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
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
    /// Tests that PostWithSettingsResponseAsync throws ArgumentNullException when settings is null.
    /// </summary>
    [Fact]
    public async Task PostWithSettingsResponseAsync_ThrowsArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
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
    /// Tests that PostWithExportedProjectAsync correctly copies files from ExportSnapshotResponse to ImportSnapshotRequest.
    /// </summary>
    [Fact]
    public async Task PostWithExportedProjectAsync_CopiesFilesCorrectly()
    {
        // Arrange - Create an exported project with files in AdditionalData
        var exportedProject = new ExportSnapshotResponse
        {
            Files = new ExportSnapshotResponse_files()
        };

        // Add some mock file data to AdditionalData
        exportedProject.Files.AdditionalData["project.json"] = "project data";
        exportedProject.Files.AdditionalData["flows.json"] = "flows data";
        exportedProject.Files.AdditionalData["settings.json"] = "settings data";

        // Use CreateWithStreamAsserter to capture the actual request
        ImportSnapshotRequest? actualRequest = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<ImportSnapshotRequest>(
            (requestInfo, request) =>
            {
                actualRequest = request;
            });

        // Act - Call the extension method
        await descopeClient.Mgmt.V1.Project.Import.PostWithExportedProjectAsync(exportedProject);

        // Assert - Verify the request was sent and files were copied correctly
        actualRequest.Should().NotBeNull("The extension method should have sent a request");
        actualRequest!.Files.Should().NotBeNull("Files should be populated");
        actualRequest.Files!.AdditionalData.Should().NotBeNull("Files AdditionalData should be populated");

        // Verify all file keys were copied
        actualRequest.Files.AdditionalData.Should().HaveCount(3, "All files should be copied");
        actualRequest.Files.AdditionalData.Should().ContainKey("project.json");
        actualRequest.Files.AdditionalData.Should().ContainKey("flows.json");
        actualRequest.Files.AdditionalData.Should().ContainKey("settings.json");

        // Verify the values match
        actualRequest.Files.AdditionalData["project.json"].Should().Be("project data");
        actualRequest.Files.AdditionalData["flows.json"].Should().Be("flows data");
        actualRequest.Files.AdditionalData["settings.json"].Should().Be("settings data");
    }

    /// <summary>
    /// Tests that PostWithExportedProjectAsync throws ArgumentNullException when exportedProject is null.
    /// </summary>
    [Fact]
    public async Task PostWithExportedProjectAsync_ThrowsArgumentNullException_WhenExportedProjectIsNull()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await descopeClient.Mgmt.V1.Project.Import.PostWithExportedProjectAsync(null!);
        });
    }

}
