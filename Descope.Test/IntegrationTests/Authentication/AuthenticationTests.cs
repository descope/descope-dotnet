using Xunit;

namespace Descope.Test.Integration
{
    public class AuthenticationTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Authentication_ValidateAndRefresh()
        {
            string? loginId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.LoginIds.First();

                // Make sure the session is valid
                var token = await _descopeClient.Auth.ValidateSession(testUser.AuthInfo.SessionJwt);
                Assert.Equal(testUser.AuthInfo.SessionJwt, token.Jwt);
                Assert.NotEmpty(token.Id);
                Assert.NotEmpty(token.ProjectId);

                // Refresh and see we got a new token
                var refreshedToken = await _descopeClient.Auth.RefreshSession(testUser.AuthInfo.RefreshJwt!);
                Assert.NotNull(refreshedToken.RefreshExpiration);
                Assert.Equal(token.Id, refreshedToken.Id);
                Assert.Equal(token.ProjectId, refreshedToken.ProjectId);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Authentication_ExchangeAccessKeyAndMe()
        {
            string? loginId = null;
            string? accessKeyId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.LoginIds.First();

                // Create an access key and exchange it
                var accessKeyResponse = await _descopeClient.Management.AccessKey.Create(loginId, userId: testUser.User.UserId);
                accessKeyId = accessKeyResponse.Key.Id;
                var token = await _descopeClient.Auth.ExchangeAccessKey(accessKeyResponse.Cleartext);
                Assert.NotEmpty(token.Id);
                Assert.NotEmpty(token.ProjectId);
                Assert.NotEmpty(token.Jwt);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(accessKeyId))
                {
                    try { await _descopeClient.Management.AccessKey.Delete(accessKeyId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Authentication_SelectTenant()
        {
            string? loginId = null;
            List<string> tenantIds = new() { };
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.LoginIds.First();

                // Create a couple of tenants and add to the user
                var tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions(Guid.NewGuid().ToString()));
                tenantIds.Add(tenantId);
                await _descopeClient.Management.User.AddTenant(loginId, tenantId);
                tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions(Guid.NewGuid().ToString()));
                tenantIds.Add(tenantId);
                await _descopeClient.Management.User.AddTenant(loginId, tenantId);
                var session = await _descopeClient.Auth.SelectTenant(tenantId, testUser.AuthInfo.RefreshJwt!);
                Assert.NotEmpty(session.SessionToken.Id);
                Assert.NotEmpty(session.SessionToken.ProjectId);
                Assert.NotEmpty(session.SessionToken.Jwt);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
                foreach (var tenantId in tenantIds)
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Authentication_MeAndLogout()
        {
            string? loginId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.LoginIds.First();

                // Me
                var user = await _descopeClient.Auth.Me(testUser.AuthInfo.RefreshJwt!);
                Assert.Equal(testUser.User.UserId, user.UserId);

                // Logout
                await _descopeClient.Auth.LogOut(testUser.AuthInfo.RefreshJwt!);

                // Try me again
                async Task Act() => await _descopeClient.Auth.Me(testUser.AuthInfo.RefreshJwt!);
                DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("Expired due to logout", result.Message);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Authentication_EnchantedLink()
        {
            string? loginId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.LoginIds.First();

                // Enchanted link sign in
                //var enchantedLinkResponse = await _descopeClient.Auth.EnchantedLink.SignIn(loginId, "https://example.com", new LoginOptions {}, testUser.AuthInfo.RefreshJwt!);
                var enchantedLinkResponse = await _descopeClient.Auth.EnchantedLink.SignIn(loginId, null, new LoginOptions { }, null);
                Assert.NotEmpty(enchantedLinkResponse.LinkId);
                Assert.NotEmpty(enchantedLinkResponse.MaskedEmail);
                Assert.NotEmpty(enchantedLinkResponse.PendingRef);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Signup_EnchantedLink()
        {
            string? loginId = null;
            try
            {
                loginId = $"tester+{Guid.NewGuid().ToString()}@test.com";

                // Enchanted link sign up
                var enchantedLinkResponse = await _descopeClient.Auth.EnchantedLink.SignUp(loginId, "https://example.com",
                new SignUpDetails { Email = loginId },
                new SignUpOptions { TemplateID = "test", TemplateOptions = new Dictionary<string, string> { { "key", "value" } } });
                Assert.NotEmpty(enchantedLinkResponse.LinkId);
                Assert.NotEmpty(enchantedLinkResponse.MaskedEmail);
                Assert.NotEmpty(enchantedLinkResponse.PendingRef);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
            }
        }
    }
}
