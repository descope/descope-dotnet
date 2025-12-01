using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    public class SsoTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Sso_SamlSetAndDelete()
        {
            string? tenantId = null;
            string? roleName = null;
            try
            {
                // Create a tenant
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenantResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);
                tenantId = tenantResponse?.Id;

                // Create a role
                roleName = Guid.NewGuid().ToString()[..20];
                var createRoleRequest = new CreateRoleRequest
                {
                    Name = roleName,
                    TenantId = tenantId
                };
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(createRoleRequest);

                // Update sso settings
                var settings = new SSOSAMLSettings
                {
                    IdpUrl = "https://sometestidp.com",
                    EntityId = "entityId",
                    IdpCert = "cert",
                    RoleMappings = new List<RoleMapping>
                    {
                        new RoleMapping
                        {
                            Groups = new List<string> { "group1", "group2" },
                            RoleName = roleName
                        }
                    }
                };
                var configureSamlRequest = new ConfigureSSOSAMLSettingsRequest
                {
                    TenantId = tenantId,
                    Settings = settings,
                    RedirectUrl = "https://myredirect.com",
                    Domains = new List<string> { "domain1.com" }
                };
                await _descopeClient.Mgmt.V1.Sso.Saml.PostAsync(configureSamlRequest);

                // Load settings
                var loadedSetting = await _descopeClient.Mgmt.V2.Sso.Settings.LoadAsync(tenantId!);

                // Make sure the settings match
                Assert.Equal(settings.IdpUrl, loadedSetting?.Saml?.IdpSSOUrl);
                Assert.Equal(settings.EntityId, loadedSetting?.Saml?.IdpEntityId);
                Assert.Equal(settings.IdpCert, loadedSetting?.Saml?.IdpCertificate);
                Assert.NotEmpty(loadedSetting?.Saml?.GroupsMapping?.First()?.Role?.Id ?? "");
                Assert.Equal("group1", loadedSetting?.Saml?.GroupsMapping?.First()?.Groups?[0]);
                Assert.Equal("group2", loadedSetting?.Saml?.GroupsMapping?.First()?.Groups?[1]);
                Assert.Equal("https://myredirect.com", loadedSetting?.Saml?.RedirectUrl);
                Assert.Equal("domain1.com", loadedSetting?.Tenant?.Domains?.First());

                // Delete the settings
                await _descopeClient.Mgmt.V1.Sso.Settings.DeleteWithTenantIdAsync(tenantId!);
                loadedSetting = await _descopeClient.Mgmt.V2.Sso.Settings.LoadAsync(tenantId!);
                Assert.Empty(loadedSetting?.Saml?.IdpSSOUrl ?? "");
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = roleName }); }
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
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenantResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);
                tenantId = tenantResponse?.Id;

                // Create a role
                roleName = Guid.NewGuid().ToString()[..20];
                var createRoleRequest = new CreateRoleRequest
                {
                    Name = roleName,
                    TenantId = tenantId
                };
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(createRoleRequest);

                // Update sso settings
                var settings = new SSOSAMLByMetadataSettings
                {
                    IdpMetadataUrl = "https://sometestidpmd.com",
                    RoleMappings = new List<RoleMapping>
                    {
                        new RoleMapping
                        {
                            Groups = new List<string> { "group1", "group2" },
                            RoleName = roleName
                        }
                    }
                };
                var configureSamlByMetadataRequest = new ConfigureSSOSAMLSettingsByMetadataRequest
                {
                    TenantId = tenantId,
                    Settings = settings,
                    RedirectUrl = "https://myredirect.com",
                    Domains = new List<string> { "domain1.com" }
                };
                await _descopeClient.Mgmt.V1.Sso.Saml.Metadata.PostAsync(configureSamlByMetadataRequest);

                // Load settings
                var loadedSetting = await _descopeClient.Mgmt.V2.Sso.Settings.LoadAsync(tenantId!);

                // Make sure the settings match
                Assert.Equal(settings.IdpMetadataUrl, loadedSetting?.Saml?.IdpMetadataUrl);
                Assert.NotEmpty(loadedSetting?.Saml?.GroupsMapping?.First()?.Role?.Id ?? "");
                Assert.Equal("group1", loadedSetting?.Saml?.GroupsMapping?.First()?.Groups?[0]);
                Assert.Equal("group2", loadedSetting?.Saml?.GroupsMapping?.First()?.Groups?[1]);
                Assert.Equal("https://myredirect.com", loadedSetting?.Saml?.RedirectUrl);
                Assert.Equal("domain1.com", loadedSetting?.Tenant?.Domains?.First());
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = roleName }); }
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
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenantResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);
                tenantId = tenantResponse?.Id;

                // Create a role
                roleName = Guid.NewGuid().ToString()[..20];
                var createRoleRequest = new CreateRoleRequest
                {
                    Name = roleName,
                    TenantId = tenantId
                };
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(createRoleRequest);

                // Update sso settings
                var settings = new SSOOIDCSettings
                {
                    Name = "Name",
                    ClientId = "ClientId",
                    ClientSecret = "ClientSecret",
                    AuthUrl = "https://mytestauth.com",
                    TokenUrl = "https://mytestauth.com",
                    JWKsUrl = "https://mytestauth.com",
                    UserAttrMapping = new OAuthUserDataClaimsMapping { }
                };
                var configureOidcRequest = new ConfigureSSOOIDCSettingsRequest
                {
                    TenantId = tenantId,
                    Settings = settings,
                    Domains = new List<string> { "domain1.com" }
                };
                await _descopeClient.Mgmt.V1.Sso.Oidc.PostAsync(configureOidcRequest);

                // Load settings
                var loadedSetting = await _descopeClient.Mgmt.V2.Sso.Settings.LoadAsync(tenantId!);

                // Make sure the settings match
                Assert.Equal(settings.Name, loadedSetting?.Oidc?.Name);
                Assert.Equal(settings.ClientId, loadedSetting?.Oidc?.ClientId);
                Assert.Equal(settings.AuthUrl, loadedSetting?.Oidc?.AuthUrl);
                Assert.Equal(settings.TokenUrl, loadedSetting?.Oidc?.TokenUrl);
                Assert.Equal(settings.JWKsUrl, loadedSetting?.Oidc?.JWKsUrl);
                Assert.Equal("domain1.com", loadedSetting?.Tenant?.Domains?.First());
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = roleName }); }
                    catch { }
                }
            }
        }
    }
}
