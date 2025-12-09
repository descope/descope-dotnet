using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    public class PasswordSettingsTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task PasswordSettings_GetAndUpdate()
        {
            string? tenantId = null;
            try
            {
                // Create a tenant
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenantResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);
                tenantId = tenantResponse?.Id!;

                // Update project level
                var settings = await _descopeClient.Mgmt.V1.Password.Settings.GetForProjectAsync();
                settings!.MinLength = 6;
                await _descopeClient.Mgmt.V1.Password.Settings.PostWithSettingsResponseAsync(settings);

                // Update tenant level
                settings!.MinLength = 7;
                settings.TenantId = tenantId;
                await _descopeClient.Mgmt.V1.Password.Settings.PostWithSettingsResponseAsync(settings);

                // Make sure changes don't clash
                var projectSettings = await _descopeClient.Mgmt.V1.Password.Settings.GetForProjectAsync();
                Assert.Equal(6, projectSettings?.MinLength);

                var tenantSettings = await _descopeClient.Mgmt.V1.Password.Settings.GetWithTenantIdAsync(tenantId!);
                Assert.Equal(7, tenantSettings?.MinLength);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
            }
        }
    }
}
