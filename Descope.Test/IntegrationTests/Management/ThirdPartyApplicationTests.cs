using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    public class ThirdPartyApplicationTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task ThirdPartyApplication_CreateAndLoad()
        {
            string? appId = null;
            try
            {
                // Create a third-party application
                var appName = $"Test 3rd Party App {Guid.NewGuid()}";
                var createRequest = new CreateThirdPartyApplicationRequest
                {
                    Name = appName,
                    Description = "Integration test third-party application",
                    LoginPageUrl = "https://example.com/login"
                };

                var createResponse = await _descopeClient.Mgmt.V1.Thirdparty.App.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                Assert.NotNull(createResponse.Id);
                Assert.NotEmpty(createResponse.Id);
                appId = createResponse.Id;

                // Load the application using the endpoint with response_body: "app"
                var loadResponse = await _descopeClient.Mgmt.V1.Thirdparty.App.Load.GetWithIdAsync(appId!);

                // Verify the response structure - the "app" field should be directly accessible
                Assert.NotNull(loadResponse);
                Assert.NotNull(loadResponse.App);
                Assert.Equal(appId, loadResponse.App.Id);
                Assert.Equal(appName, loadResponse.App.Name);
                Assert.Equal(createRequest.Description, loadResponse.App.Description);
                Assert.Empty(loadResponse.App.Logo!);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Thirdparty.App.DeletePath.PostAsync(new DeleteThirdPartyApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task ThirdPartyApplication_LoadWithClientId()
        {
            string? appId = null;
            try
            {
                // Create a third-party application
                var appName = $"Test 3rd Party App {Guid.NewGuid()}";
                var createRequest = new CreateThirdPartyApplicationRequest
                {
                    Name = appName,
                    Description = "Integration test third-party application",
                    LoginPageUrl = "https://example.com/login"
                };

                var createResponse = await _descopeClient.Mgmt.V1.Thirdparty.App.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                appId = createResponse.Id;
                var clientId = createResponse.ClientId;

                // Load by clientId
                var loadResponse = await _descopeClient.Mgmt.V1.Thirdparty.App.Load.GetWithClientIdAsync(clientId!);

                // Verify the response structure
                Assert.NotNull(loadResponse);
                Assert.NotNull(loadResponse.App);
                Assert.Equal(appId, loadResponse.App.Id);
                Assert.Equal(clientId, loadResponse.App.ClientId);
                Assert.Equal(appName, loadResponse.App.Name);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Thirdparty.App.DeletePath.PostAsync(new DeleteThirdPartyApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }
    }
}
