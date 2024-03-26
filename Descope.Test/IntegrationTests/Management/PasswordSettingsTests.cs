using Xunit;

namespace Descope.Test.Integration
{
    public class PasswordSettingsTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task PasswordSettings_GetAndUpdate()
        {
            string? tenantId = null;
            try
            {
                // Create a tenant
                tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions(Guid.NewGuid().ToString()));

                // update project level
                var settings = await _descopeClient.Management.Password.GetSettings();
                settings.MinLength = 6;
                await _descopeClient.Management.Password.ConfigureSettings(settings);

                // update tenant level
                settings.MinLength = 7;
                await _descopeClient.Management.Password.ConfigureSettings(settings, tenantId);

                // make sure changes don't clash
                var projectSettings = await _descopeClient.Management.Password.GetSettings();
                Assert.Equal(6, projectSettings.MinLength);
                var tenantSettings = await _descopeClient.Management.Password.GetSettings(tenantId);
                Assert.Equal(7, tenantSettings.MinLength);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
                    catch { }
                }
            }
        }
    }
}
