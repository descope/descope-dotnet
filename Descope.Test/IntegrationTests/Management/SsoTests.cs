using Xunit;

namespace Descope.Test.Integration
{
    public class SsoTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Sso_SamlSetAndDelete()
        {
            string? tenantId = null;
            string? roleName = null;
            try
            {
                // Create a tenant
                tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions(Guid.NewGuid().ToString()));
                roleName = Guid.NewGuid().ToString()[..20];
                await _descopeClient.Management.Role.Create(roleName, tenantId: tenantId);

                // Update sso settings
                var settings = new SsoSamlSettings("https://sometestidp.com", "entityId", "cert")
                {
                    RoleMappings = new List<RoleMapping> { new RoleMapping(new List<string> { "group1", "group2" }, roleName) }
                };
                await _descopeClient.Management.Sso.ConfigureSAMLSettings(tenantId, settings, "https://myredirect.com", new List<string> { "domain1.com" });

                var loadedSetting = await _descopeClient.Management.Sso.LoadSettings(tenantId);

                // Make sure the settings match
                Assert.Equal(settings.IdpUrl, loadedSetting.Saml?.IdpSsoUrl);
                Assert.Equal(settings.IdpEntityId, loadedSetting.Saml?.IdpEntityId);
                Assert.Equal(settings.IdpCertificate, loadedSetting.Saml!.IdpCertificate);
                Assert.NotEmpty(loadedSetting.Saml.GroupsMapping!.First().Role!.Id);
                Assert.Equal("group1", loadedSetting.Saml.GroupsMapping!.First().Groups![0]);
                Assert.Equal("group2", loadedSetting.Saml.GroupsMapping!.First().Groups![1]);
                Assert.Equal("https://myredirect.com", loadedSetting.Saml?.RedirectUrl);
                Assert.Equal("domain1.com", loadedSetting.Tenant.Domains.First());

                // Delete the settings
                await _descopeClient.Management.Sso.DeleteSettings(tenantId);
                loadedSetting = await _descopeClient.Management.Sso.LoadSettings(tenantId);
                Assert.Empty(loadedSetting.Saml.IdpSsoUrl ?? "");
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Management.Role.Delete(roleName); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Sso_SamlByMetadata()
        {
            string? tenantId = null;
            string? roleName = null;
            try
            {
                // Create a tenant
                tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions(Guid.NewGuid().ToString()));
                roleName = Guid.NewGuid().ToString()[..20];
                await _descopeClient.Management.Role.Create(roleName, tenantId: tenantId);

                // update sso settings
                var settings = new SsoSamlSettingsByMetadata("https://sometestidpmd.com")
                {
                    RoleMappings = new List<RoleMapping> { new RoleMapping(new List<string> { "group1", "group2" }, roleName) }
                };
                await _descopeClient.Management.Sso.ConfigureSamlSettingsByMetadata(tenantId, settings, "https://myredirect.com", new List<string> { "domain1.com" });

                var loadedSetting = await _descopeClient.Management.Sso.LoadSettings(tenantId);

                // Make sure the settings match
                Assert.Equal(settings.IdpMetadataUrl, loadedSetting.Saml.IdpMetadataUrl);
                Assert.NotEmpty(loadedSetting.Saml.GroupsMapping?.First()?.Role?.Id ?? "");
                Assert.Equal("group1", loadedSetting.Saml.GroupsMapping?.First()?.Groups?[0]);
                Assert.Equal("group2", loadedSetting.Saml.GroupsMapping?.First()?.Groups?[1]);
                Assert.Equal("https://myredirect.com", loadedSetting.Saml?.RedirectUrl);
                Assert.Equal("domain1.com", loadedSetting.Tenant.Domains.First());
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Management.Role.Delete(roleName); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Sso_Oidc()
        {
            string? tenantId = null;
            string? roleName = null;
            try
            {
                // Create a tenant
                tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions(Guid.NewGuid().ToString()));
                roleName = Guid.NewGuid().ToString()[..20];
                await _descopeClient.Management.Role.Create(roleName, tenantId: tenantId);

                // Update sso settings
                var settings = new SsoOidcSettings
                {
                    Name = "Name",
                    ClientId = "ClientId",
                    ClientSecret = "ClientSecret",
                    AuthUrl = "https://mytestauth.com",
                    TokenUrl = "https://mytestauth.com",
                    JwksUrl = "https://mytestauth.com",
                    AttributeMapping = new OidcAttributeMapping { }
                };
                await _descopeClient.Management.Sso.ConfigureOidcSettings(tenantId, settings, new List<string> { "domain1.com" });

                var loadedSetting = await _descopeClient.Management.Sso.LoadSettings(tenantId);

                // Make sure the settings match
                Assert.Equal(settings.Name, loadedSetting.Oidc.Name);
                Assert.Equal(settings.ClientId, loadedSetting.Oidc.ClientId);
                Assert.Equal(settings.AuthUrl, loadedSetting.Oidc.AuthUrl);
                Assert.Equal(settings.TokenUrl, loadedSetting.Oidc.TokenUrl);
                Assert.Equal(settings.JwksUrl, loadedSetting.Oidc.JwksUrl);
                Assert.Equal("domain1.com", loadedSetting.Tenant.Domains.First());
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Management.Role.Delete(roleName); }
                    catch { }
                }
            }
        }
    }
}
