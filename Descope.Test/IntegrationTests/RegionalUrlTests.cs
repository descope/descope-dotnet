using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    public class RegionalUrlTests
    {

        [Fact(Skip = "Manual testing only - allows verifying explicit URL logic by inspecting network calls")]
        //[Fact]
        public async Task RegionalUrl_WithExplicitURL_ShouldUseURL()
        {
            // Arrange
            var options = IntegrationTestSetup.GetDescopeClientOptions();
            // Override with a use1 region project ID (32+ characters starting with "Puse1") but also set explicit URL
            options.ProjectId = "Puse1567890123456789012345678901"; // Example use1 project ID
            options.BaseUrl = "https://api.euc1.descope.com"; // Example explicit regional URL

            // Act
            var client = DescopeManagementClientFactory.Create(options);

            // Make a simple API call to verify the URL works
            var searchRequest = new SearchUsersRequest
            {
                Limit = 1
            };
            await client.Mgmt.V2.User.Search.PostAsync(searchRequest);
        }

        [Fact(Skip = "Manual testing only - allows verifying default URL logic by inspecting network calls")]
        //[Fact]
        public async Task RegionalUrl_WithDefaultProjectId_ShouldUseDefaultUrl()
        {
            // Arrange
            var options = IntegrationTestSetup.GetDescopeClientOptions();
            options.BaseUrl = null; // Clear BaseUrl to trigger default URL logic

            // Act
            var client = DescopeManagementClientFactory.Create(options);

            // Make a simple API call to verify the URL works
            var searchRequest = new SearchUsersRequest
            {
                Limit = 1
            };
            await client.Mgmt.V2.User.Search.PostAsync(searchRequest);
        }

        [Fact(Skip = "Manual testing only - allows verifying regional URL logic by inspecting network calls")]
        //[Fact]
        public async Task RegionalUrl_WithUse1ProjectId_ShouldUseRegionalUrl()
        {
            // Arrange
            var options = IntegrationTestSetup.GetDescopeClientOptions();

            // Override with a use1 region project ID (32+ characters starting with "Puse1")
            options.ProjectId = "Puse1567890123456789012345678901"; // Example use1 project ID
            options.BaseUrl = null; // Clear BaseUrl to trigger automatic region-based logic

            // Act
            var client = DescopeManagementClientFactory.Create(options);

            // Make a simple API call to verify the regional URL works
            var searchRequest = new SearchUsersRequest
            {
                Limit = 1
            };
            await client.Mgmt.V2.User.Search.PostAsync(searchRequest);
        }
    }
}
