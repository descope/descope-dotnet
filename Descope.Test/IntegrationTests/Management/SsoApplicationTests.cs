using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class SsoApplicationTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task SsoApplication_CreateOIDCApplicationAndLoad()
        {
            string? appId = null;
            try
            {
                // Create an OIDC application
                var createRequest = new CreateUpdateSSOOIDCApplicationRequest
                {
                    Name = $"Test OIDC App {Guid.NewGuid()}",
                    Description = "Integration test OIDC application",
                    Enabled = true,
                    Logo = "https://example.com/logo.png",
                    LoginPageUrl = "https://example.com/login",
                    ForceAuthentication = true
                };

                var createResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Oidc.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                Assert.NotNull(createResponse.Id);
                Assert.NotEmpty(createResponse.Id);
                appId = createResponse.Id;

                // Load and verify
                await RetryUntilSuccessAsync(async () =>
                {
                    var loadResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync(appId!);
                    Assert.NotNull(loadResponse);
                    Assert.NotNull(loadResponse.App);
                    Assert.NotNull(loadResponse.App.Id);
                    Assert.Equal(appId, loadResponse.App.Id);
                    Assert.Equal(createRequest.Name, loadResponse.App.Name);
                    Assert.Equal(createRequest.Description, loadResponse.App.Description);
                    Assert.Equal(createRequest.Enabled, loadResponse.App.Enabled);
                    Assert.Equal(createRequest.Logo, loadResponse.App.Logo);
                    Assert.Equal("oidc", loadResponse.App.AppType);
                    Assert.NotNull(loadResponse.App.OidcSettings);
                    Assert.Equal(createRequest.LoginPageUrl, loadResponse.App.OidcSettings.LoginPageUrl);
                    Assert.Equal(createRequest.ForceAuthentication, loadResponse.App.OidcSettings.ForceAuthentication);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task SsoApplication_CreateSAMLApplicationAndLoad()
        {
            string? appId = null;
            try
            {
                // Create a SAML application
                var createRequest = new CreateUpdateSSOSAMLApplicationRequest
                {
                    Name = $"Test SAML App {Guid.NewGuid()}",
                    Description = "Integration test SAML application",
                    Enabled = true,
                    Logo = "https://example.com/logo.png",
                    LoginPageUrl = "https://example.com/login",
                    UseMetadataInfo = true,
                    MetadataUrl = "https://example.com/metadata",
                    EntityId = "test-entity-id",
                    AcsUrl = "https://example.com/acs",
                    ForceAuthentication = true
                };

                var createResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Saml.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                Assert.NotNull(createResponse.Id);
                Assert.NotEmpty(createResponse.Id);
                appId = createResponse.Id;

                // Load and verify
                await RetryUntilSuccessAsync(async () =>
                {
                    var loadResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync(appId!);
                    Assert.NotNull(loadResponse);
                    Assert.NotNull(loadResponse.App);
                    Assert.Equal(appId, loadResponse.App.Id);
                    Assert.Equal(createRequest.Name, loadResponse.App.Name);
                    Assert.Equal(createRequest.Description, loadResponse.App.Description);
                    Assert.Equal(createRequest.Enabled, loadResponse.App.Enabled);
                    Assert.Equal(createRequest.Logo, loadResponse.App.Logo);
                    Assert.Equal("saml", loadResponse.App.AppType);
                    Assert.NotNull(loadResponse.App.SamlSettings);
                    Assert.Equal(createRequest.LoginPageUrl, loadResponse.App.SamlSettings.LoginPageUrl);
                    Assert.Equal(createRequest.UseMetadataInfo, loadResponse.App.SamlSettings.UseMetadataInfo);
                    Assert.Equal(createRequest.MetadataUrl, loadResponse.App.SamlSettings.MetadataUrl);
                    Assert.Equal(createRequest.EntityId, loadResponse.App.SamlSettings.EntityId);
                    Assert.Equal(createRequest.AcsUrl, loadResponse.App.SamlSettings.AcsUrl);
                    Assert.Equal(createRequest.ForceAuthentication, loadResponse.App.SamlSettings.ForceAuthentication);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task SsoApplication_UpdateOIDCApplication()
        {
            string? appId = null;
            try
            {
                // Create an OIDC application
                var createRequest = new CreateUpdateSSOOIDCApplicationRequest
                {
                    Name = $"Test OIDC App {Guid.NewGuid()}",
                    Description = "Original description",
                    Enabled = true
                };

                var createResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Oidc.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                appId = createResponse.Id;

                // Update the application
                var updateRequest = new CreateUpdateSSOOIDCApplicationRequest
                {
                    Id = appId,
                    Name = $"Updated OIDC App {Guid.NewGuid()}",
                    Description = "Updated description",
                    Enabled = false,
                    Logo = "https://example.com/new-logo.png",
                    LoginPageUrl = "https://example.com/new-login",
                    ForceAuthentication = false
                };

                await _descopeClient.Mgmt.V1.Sso.Idp.App.Oidc.Update.PostAsync(updateRequest);

                // Load and verify the update
                var loadResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync(appId!);
                Assert.NotNull(loadResponse);
                Assert.NotNull(loadResponse.App);
                Assert.Equal(updateRequest.Name, loadResponse.App.Name);
                Assert.Equal(updateRequest.Description, loadResponse.App.Description);
                Assert.Equal(updateRequest.Enabled, loadResponse.App.Enabled);
                Assert.Equal(updateRequest.Logo, loadResponse.App.Logo);
                Assert.NotNull(loadResponse.App.OidcSettings);
                Assert.Equal(updateRequest.LoginPageUrl, loadResponse.App.OidcSettings.LoginPageUrl);
                Assert.Equal(updateRequest.ForceAuthentication, loadResponse.App.OidcSettings.ForceAuthentication);
            }
            finally
            {
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task SsoApplication_UpdateSAMLApplication()
        {
            string? appId = null;
            try
            {
                // Create a SAML application
                var createRequest = new CreateUpdateSSOSAMLApplicationRequest
                {
                    Name = $"Test SAML App {Guid.NewGuid()}",
                    Description = "Original description",
                    Enabled = true,
                    UseMetadataInfo = true,
                    MetadataUrl = "https://example.com/metadata",
                    EntityId = "original-entity-id",
                    AcsUrl = "https://example.com/acs"
                };

                var createResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Saml.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                appId = createResponse.Id;

                // Update the application
                var updateRequest = new CreateUpdateSSOSAMLApplicationRequest
                {
                    Id = appId,
                    Name = $"Updated SAML App {Guid.NewGuid()}",
                    Description = "Updated description",
                    Enabled = false,
                    Logo = "https://example.com/new-logo.png",
                    LoginPageUrl = "https://example.com/new-login",
                    UseMetadataInfo = false,
                    EntityId = "updated-entity-id",
                    AcsUrl = "https://example.com/new-acs",
                    ForceAuthentication = true
                };

                await _descopeClient.Mgmt.V1.Sso.Idp.App.Saml.Update.PostAsync(updateRequest);

                // Load and verify the update
                var loadResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync(appId!);
                Assert.NotNull(loadResponse);
                Assert.NotNull(loadResponse.App);
                Assert.Equal(updateRequest.Name, loadResponse.App.Name);
                Assert.Equal(updateRequest.Description, loadResponse.App.Description);
                Assert.Equal(updateRequest.Enabled, loadResponse.App.Enabled);
                Assert.Equal(updateRequest.Logo, loadResponse.App.Logo);
                Assert.NotNull(loadResponse.App.SamlSettings);
                Assert.Equal(updateRequest.LoginPageUrl, loadResponse.App.SamlSettings.LoginPageUrl);
                Assert.Equal(updateRequest.UseMetadataInfo, loadResponse.App.SamlSettings.UseMetadataInfo);
                Assert.Equal(updateRequest.EntityId, loadResponse.App.SamlSettings.EntityId);
                Assert.Equal(updateRequest.AcsUrl, loadResponse.App.SamlSettings.AcsUrl);
                Assert.Equal(updateRequest.ForceAuthentication, loadResponse.App.SamlSettings.ForceAuthentication);
            }
            finally
            {
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task SsoApplication_DeleteAndVerify()
        {
            string? appId = null;
            try
            {
                // Create an OIDC application
                var createRequest = new CreateUpdateSSOOIDCApplicationRequest
                {
                    Name = $"Test OIDC App {Guid.NewGuid()}",
                    Enabled = true
                };

                var createResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Oidc.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                appId = createResponse.Id;

                // Delete the application
                await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = appId });
                appId = null; // Clear to avoid cleanup

                // Verify deletion by trying to load all apps and ensuring our app is not there
                var loadAllResponse = await _descopeClient.Mgmt.V1.Sso.Idp.Apps.Load.GetAsync();
                Assert.NotNull(loadAllResponse);
                var deletedApp = loadAllResponse.Apps?.FirstOrDefault(app => app.Id == createResponse.Id);
                Assert.Null(deletedApp);
            }
            finally
            {
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task SsoApplication_LoadAll()
        {
            string? oidcAppId = null;
            string? samlAppId = null;
            try
            {
                // Create an OIDC application
                var oidcRequest = new CreateUpdateSSOOIDCApplicationRequest
                {
                    Name = $"Test OIDC App {Guid.NewGuid()}",
                    Description = "OIDC test app",
                    Enabled = true
                };
                var oidcResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Oidc.Create.PostAsync(oidcRequest);
                Assert.NotNull(oidcResponse);
                oidcAppId = oidcResponse.Id;

                // Create a SAML application
                var samlRequest = new CreateUpdateSSOSAMLApplicationRequest
                {
                    Name = $"Test SAML App {Guid.NewGuid()}",
                    Description = "SAML test app",
                    Enabled = true,
                    UseMetadataInfo = true,
                    MetadataUrl = "https://example.com/metadata",
                    EntityId = "test-entity",
                    AcsUrl = "https://example.com/acs"
                };
                var samlResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Saml.Create.PostAsync(samlRequest);
                Assert.NotNull(samlResponse);
                samlAppId = samlResponse.Id;

                // Load all applications
                var loadAllResponse = await _descopeClient.Mgmt.V1.Sso.Idp.Apps.Load.GetAsync();
                Assert.NotNull(loadAllResponse);
                Assert.NotNull(loadAllResponse.Apps);

                // Verify our apps are in the list
                var foundOidcApp = loadAllResponse.Apps.FirstOrDefault(app => app.Id == oidcAppId);
                Assert.NotNull(foundOidcApp);
                Assert.Equal("oidc", foundOidcApp.AppType);
                Assert.Equal(oidcRequest.Name, foundOidcApp.Name);
                Assert.NotNull(foundOidcApp.OidcSettings);

                var foundSamlApp = loadAllResponse.Apps.FirstOrDefault(app => app.Id == samlAppId);
                Assert.NotNull(foundSamlApp);
                Assert.Equal("saml", foundSamlApp.AppType);
                Assert.Equal(samlRequest.Name, foundSamlApp.Name);
                Assert.NotNull(foundSamlApp.SamlSettings);
            }
            finally
            {
                if (!string.IsNullOrEmpty(oidcAppId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = oidcAppId });
                    }
                    catch { }
                }
                if (!string.IsNullOrEmpty(samlAppId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = samlAppId });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task SsoApplication_CreateOIDCWithJWTBearerSettings()
        {
            string? appId = null;
            try
            {
                // Create an OIDC application with JWT Bearer settings
                // Note: JWTBearerSettings uses AdditionalData for issuers, so we set it directly
                var jwtBearerSettings = new JWTBearerSettings
                {
                    Issuers = new JWTBearerSettings_issuers
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["issuer1"] = new Dictionary<string, object>
                            {
                                ["jwksUri"] = "https://example.com/jwks",
                                ["signAlgorithm"] = "RS256",
                                ["userInfoUri"] = "https://example.com/userinfo",
                                ["externalIdFieldName"] = "sub"
                            }
                        }
                    }
                };

                var createRequest = new CreateUpdateSSOOIDCApplicationRequest
                {
                    Name = $"Test OIDC JWT App {Guid.NewGuid()}",
                    Description = "OIDC app with JWT Bearer settings",
                    Enabled = true,
                    ForceAuthentication = true,
                    JwtBearerSettings = jwtBearerSettings,
                    BackChannelLogoutUrl = "https://example.com/backchannel-logout"
                };

                var createResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Oidc.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                Assert.NotNull(createResponse.Id);
                Assert.NotEmpty(createResponse.Id);
                appId = createResponse.Id;

                // Load and verify JWT Bearer settings
                var loadResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync(appId!);
                Assert.NotNull(loadResponse);
                Assert.NotNull(loadResponse.App);
                Assert.NotNull(loadResponse.App.OidcSettings);
                Assert.Equal(createRequest.ForceAuthentication, loadResponse.App.OidcSettings.ForceAuthentication);
                Assert.Equal(createRequest.BackChannelLogoutUrl, loadResponse.App.OidcSettings.BackChannelLogoutUrl);
                Assert.NotNull(loadResponse.App.OidcSettings.JwtBearerSettings);
                Assert.NotNull(loadResponse.App.OidcSettings.JwtBearerSettings.Issuers);
                Assert.NotNull(loadResponse.App.OidcSettings.JwtBearerSettings.Issuers.AdditionalData);
                Assert.True(loadResponse.App.OidcSettings.JwtBearerSettings.Issuers.AdditionalData.ContainsKey("issuer1"));

                // Verify issuer data is present
                var issuerData = loadResponse.App.OidcSettings.JwtBearerSettings.Issuers.AdditionalData["issuer1"];
                Assert.NotNull(issuerData);
            }
            finally
            {
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task SsoApplication_CreateSAMLWithAttributeMapping()
        {
            string? appId = null;
            try
            {
                // Create a SAML application with attribute mapping
                var attributeMapping = new List<SAMLIDPAttributeMappingInfo>
                {
                    new SAMLIDPAttributeMappingInfo
                    {
                        Name = "email",
                        Type = "user_attribute",
                        Value = "email"
                    },
                    new SAMLIDPAttributeMappingInfo
                    {
                        Name = "firstName",
                        Type = "user_attribute",
                        Value = "givenName"
                    }
                };

                var createRequest = new CreateUpdateSSOSAMLApplicationRequest
                {
                    Name = $"Test SAML Attr App {Guid.NewGuid()}",
                    Description = "SAML app with attribute mapping",
                    Enabled = true,
                    UseMetadataInfo = true,
                    MetadataUrl = "https://example.com/metadata",
                    EntityId = "test-entity-id",
                    AcsUrl = "https://example.com/acs",
                    AttributeMapping = attributeMapping,
                    SubjectNameIdType = "email",
                    ForceAuthentication = true
                };

                var createResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Saml.Create.PostAsync(createRequest);
                Assert.NotNull(createResponse);
                Assert.NotNull(createResponse.Id);
                Assert.NotEmpty(createResponse.Id);
                appId = createResponse.Id;

                // Load and verify attribute mapping
                var loadResponse = await _descopeClient.Mgmt.V1.Sso.Idp.App.Load.GetWithIdAsync(appId!);
                Assert.NotNull(loadResponse);
                Assert.NotNull(loadResponse.App);
                Assert.NotNull(loadResponse.App.SamlSettings);
                Assert.NotNull(loadResponse.App.SamlSettings.AttributeMapping);
                Assert.Equal(2, loadResponse.App.SamlSettings.AttributeMapping.Count);
                Assert.Equal("email", loadResponse.App.SamlSettings.SubjectNameIdType);
                Assert.Equal(createRequest.ForceAuthentication, loadResponse.App.SamlSettings.ForceAuthentication);

                var emailMapping = loadResponse.App.SamlSettings.AttributeMapping.FirstOrDefault(m => m.Name == "email");
                Assert.NotNull(emailMapping);
                Assert.Equal("user_attribute", emailMapping.Type);
                Assert.Equal("email", emailMapping.Value);
            }
            finally
            {
                if (!string.IsNullOrEmpty(appId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Sso.Idp.App.DeletePath.PostAsync(new DeleteSSOApplicationRequest { Id = appId });
                    }
                    catch { }
                }
            }
        }
    }
}
