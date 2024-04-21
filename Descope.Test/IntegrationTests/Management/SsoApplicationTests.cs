using Xunit;

namespace Descope.Test.Integration
{
    public class SsoApplicationTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task SsoApplication_Saml()
        {
            string? id = null;
            try
            {
                var name = "saml_app";
                var url = "https://sometestidp.com";
                // Create
                var options = new SamlApplicationOptions(name, url);
                id = await _descopeClient.Management.SsoApplication.CreateSAMLApplication(options);

                // Load
                var loadedApp = await _descopeClient.Management.SsoApplication.Load(id);
                Assert.Equal(name, loadedApp.Name);
                Assert.Equal(url, loadedApp.SamlSettings!.LoginPageUrl);

                // Update
                name = "saml_app_updated";
                url = "https://sometestidp.com/updated";
                options = new SamlApplicationOptions(name, url) { Id = id };
                await _descopeClient.Management.SsoApplication.UpdateSamlApplication(options);

                // Load All
                var apps = await _descopeClient.Management.SsoApplication.LoadAll();
                loadedApp = apps.Find(a => a.Id == id);
                Assert.Equal(name, loadedApp!.Name);
                Assert.Equal(url, loadedApp.SamlSettings!.LoginPageUrl);

                // Delete
                await _descopeClient.Management.SsoApplication.Delete(id);
                id = null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try { await _descopeClient.Management.SsoApplication.Delete(id); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task SsoApplication_Oidc()
        {
            string? id = null;
            try
            {
                var name = "oidc_app";
                var url = "https://sometestidp.com";
                // Create
                var options = new OidcApplicationOptions(name, url);
                id = await _descopeClient.Management.SsoApplication.CreateOidcApplication(options);

                // Load
                var loadedApp = await _descopeClient.Management.SsoApplication.Load(id);
                Assert.Equal(name, loadedApp.Name);
                Assert.Equal(url, loadedApp.OidcSettings!.LoginPageUrl);

                // Update
                name = "oidc_app_updated";
                url = "https://sometestidp.com/updated";
                options = new OidcApplicationOptions(name, url) { Id = id };
                await _descopeClient.Management.SsoApplication.UpdateOidcApplication(options);

                // Load All
                var apps = await _descopeClient.Management.SsoApplication.LoadAll();
                loadedApp = apps.Find(a => a.Id == id);
                Assert.Equal(name, loadedApp!.Name);
                Assert.Equal(url, loadedApp.OidcSettings!.LoginPageUrl);

                // Delete
                await _descopeClient.Management.SsoApplication.Delete(id);
                id = null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try { await _descopeClient.Management.SsoApplication.Delete(id); }
                    catch { }
                }
            }
        }

    }

}
