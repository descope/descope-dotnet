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
                loginId = testUser.User.loginIds.First();

                // Make sure the session is valid
                var token = await _descopeClient.Auth.ValidateSession(testUser.AuthInfo.sessionJwt);
                Assert.Equal(testUser.AuthInfo.sessionJwt, token.Jwt);
                Assert.NotEmpty(token.Id);
                Assert.NotEmpty(token.ProjectId);

                // Refresh and see we got a new token
                var refreshedToken = await _descopeClient.Auth.RefreshSession(testUser.AuthInfo.refreshJwt!);
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
                loginId = testUser.User.loginIds.First();

                // Create an access key and exchange it
                var accessKeyResponse = await _descopeClient.Management.AccessKey.Create(loginId, userId: testUser.User.userId);
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
                loginId = testUser.User.loginIds.First();

                // Create a couple of tenants and add to the user
                var tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions { name = Guid.NewGuid().ToString() });
                tenantIds.Add(tenantId);
                await _descopeClient.Management.User.AddTenant(loginId, tenantId);
                tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions { name = Guid.NewGuid().ToString() });
                tenantIds.Add(tenantId);
                await _descopeClient.Management.User.AddTenant(loginId, tenantId);
                var session = await _descopeClient.Auth.SelectTenant(tenantId, testUser.AuthInfo.refreshJwt!);
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

        // TODO: Test permissions and roles once available on via management

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Authentication_MeAndLogout(bool logoutAll)
        {
            string? loginId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.loginIds.First();

                // Me
                var user = await _descopeClient.Auth.Me(testUser.AuthInfo.refreshJwt!);
                Assert.Equal(testUser.User.userId, user.userId);

                // Logout
                if (logoutAll) await _descopeClient.Auth.LogOut(testUser.AuthInfo.refreshJwt!);
                else await _descopeClient.Auth.LogOutAll(testUser.AuthInfo.refreshJwt!);

                // Try me again
                await Task.Delay(200);
                async Task Act() => await _descopeClient.Auth.Me(testUser.AuthInfo.refreshJwt!);
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
    }
}
