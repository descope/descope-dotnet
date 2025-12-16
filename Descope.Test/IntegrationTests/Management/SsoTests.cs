using Xunit;
using Descope.Mgmt.Models.Managementv1;
using Descope.Auth.Models.Onetimev1;

namespace Descope.Test.Integration
{
    [Collection("Project & Settings Tests")]
    [Trait("Category", "ProjectSettings")]
    public class SsoTests : RateLimitedIntegrationTest
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
                var testDomain = $"{Guid.NewGuid().ToString().Substring(0, 8)}.com";
                var configureSamlRequest = new ConfigureSSOSAMLSettingsRequest
                {
                    TenantId = tenantId,
                    Settings = settings,
                    RedirectUrl = "https://myredirect.com",
                    Domains = new List<string> { testDomain }
                };
                await _descopeClient.Mgmt.V1.Sso.Saml.PostAsync(configureSamlRequest);

                // Load settings
                var loadedSetting = await _descopeClient.Mgmt.V2.Sso.Settings.GetWithTenantIdAsync(tenantId!);

                // Make sure the settings match
                Assert.Equal(settings.IdpUrl, loadedSetting?.Saml?.IdpSSOUrl);
                Assert.Equal(settings.EntityId, loadedSetting?.Saml?.IdpEntityId);
                Assert.Equal(settings.IdpCert, loadedSetting?.Saml?.IdpCertificate);
                Assert.NotEmpty(loadedSetting?.Saml?.GroupsMapping?.First()?.Role?.Id ?? "");
                Assert.Equal("group1", loadedSetting?.Saml?.GroupsMapping?.First()?.Groups?[0]);
                Assert.Equal("group2", loadedSetting?.Saml?.GroupsMapping?.First()?.Groups?[1]);
                Assert.Equal("https://myredirect.com", loadedSetting?.Saml?.RedirectUrl);
                Assert.Equal(testDomain, loadedSetting?.Tenant?.Domains?.First());

                // Delete the settings
                await _descopeClient.Mgmt.V1.Sso.Settings.DeleteWithTenantIdAsync(tenantId!);
                loadedSetting = await _descopeClient.Mgmt.V2.Sso.Settings.GetWithTenantIdAsync(tenantId!);
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
                var testDomain = $"{Guid.NewGuid().ToString().Substring(0, 8)}.com";
                var configureSamlByMetadataRequest = new ConfigureSSOSAMLSettingsByMetadataRequest
                {
                    TenantId = tenantId,
                    Settings = settings,
                    RedirectUrl = "https://myredirect.com",
                    Domains = new List<string> { testDomain }
                };
                await _descopeClient.Mgmt.V1.Sso.Saml.Metadata.PostAsync(configureSamlByMetadataRequest);

                // Load settings
                var loadedSetting = await _descopeClient.Mgmt.V2.Sso.Settings.GetWithTenantIdAsync(tenantId!);

                // Make sure the settings match
                Assert.Equal(settings.IdpMetadataUrl, loadedSetting?.Saml?.IdpMetadataUrl);
                Assert.NotEmpty(loadedSetting?.Saml?.GroupsMapping?.First()?.Role?.Id ?? "");
                Assert.Equal("group1", loadedSetting?.Saml?.GroupsMapping?.First()?.Groups?[0]);
                Assert.Equal("group2", loadedSetting?.Saml?.GroupsMapping?.First()?.Groups?[1]);
                Assert.Equal("https://myredirect.com", loadedSetting?.Saml?.RedirectUrl);
                Assert.Equal(testDomain, loadedSetting?.Tenant?.Domains?.First());
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
                var testDomain = $"{Guid.NewGuid().ToString().Substring(0, 8)}.com";
                var configureOidcRequest = new ConfigureSSOOIDCSettingsRequest
                {
                    TenantId = tenantId,
                    Settings = settings,
                    Domains = new List<string> { testDomain }
                };
                await _descopeClient.Mgmt.V1.Sso.Oidc.PostAsync(configureOidcRequest);

                // Load settings
                var loadedSetting = await _descopeClient.Mgmt.V2.Sso.Settings.GetWithTenantIdAsync(tenantId!);

                // Make sure the settings match
                Assert.Equal(settings.Name, loadedSetting?.Oidc?.Name);
                Assert.Equal(settings.ClientId, loadedSetting?.Oidc?.ClientId);
                Assert.Equal(settings.AuthUrl, loadedSetting?.Oidc?.AuthUrl);
                Assert.Equal(settings.TokenUrl, loadedSetting?.Oidc?.TokenUrl);
                Assert.Equal(settings.JWKsUrl, loadedSetting?.Oidc?.JWKsUrl);
                Assert.Equal(testDomain, loadedSetting?.Tenant?.Domains?.First());
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
        public async Task Sso_AuthorizeEndpoint()
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
                tenantId = tenantResponse?.Id;

                // Configure SSO settings (OIDC - doesn't require certificate like SAML)
                var settings = new SSOOIDCSettings
                {
                    Name = "Test OIDC",
                    ClientId = "test-client-id",
                    ClientSecret = "test-client-secret",
                    AuthUrl = "https://testauth.example.com/authorize",
                    TokenUrl = "https://testauth.example.com/token",
                    JWKsUrl = "https://testauth.example.com/.well-known/jwks.json",
                    UserAttrMapping = new OAuthUserDataClaimsMapping { }
                };
                var testDomain = $"{Guid.NewGuid().ToString().Substring(0, 8)}.com";
                var configureOidcRequest = new ConfigureSSOOIDCSettingsRequest
                {
                    TenantId = tenantId,
                    Settings = settings,
                    Domains = new List<string> { testDomain }
                };

                await _descopeClient.Mgmt.V1.Sso.Oidc.PostAsync(configureOidcRequest);

                // Call the Auth SSO Authorize endpoint with test=true using the extension method
                var loginOptions = new LoginOptions();
                var authorizeResponse = await _descopeClient.Auth.V1.Sso.Authorize.PostWithQueryParamsAsync(
                    loginOptions,
                    tenant: tenantId!,
                    redirectUrl: "https://myredirect.com",
                    test: true);

                // Verify the response contains a redirect URL
                Assert.NotNull(authorizeResponse);
                Assert.NotEmpty(authorizeResponse.Url ?? "");
                Assert.Contains("https://", authorizeResponse.Url ?? "");
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
