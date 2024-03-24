using Xunit;

namespace Descope.Test.Integration
{
    public class JwtTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Jwt_CustomClaims()
        {
            string? loginId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.LoginIds.First();

                var updateJwt = await _descopeClient.Management.Jwt.UpdateJwtWithCustomClaims(testUser.AuthInfo.SessionJwt, new Dictionary<string, object> { { "a", "b" } });

                // Make sure the session is valid
                var token = await _descopeClient.Auth.ValidateSession(updateJwt);
                Assert.Contains("a", token.Claims.Keys);
                Assert.Equal("b", token.Claims["a"]);
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
        public async Task Jwt_Impersonate()
        {
            string? loginId = null;
            string? loginId2 = null;
            string? roleName = null;
            try
            {
                // Create a role that can impersonate
                roleName = Guid.NewGuid().ToString()[..20];
                await _descopeClient.Management.Role.Create(roleName, permissionNames: new List<string> { "Impersonate" });

                // Create impersonating user
                loginId = Guid.NewGuid().ToString();
                var response = await _descopeClient.Management.User.Create(loginId: loginId, new UserRequest()
                {
                    Phone = "+972555555555",
                    RoleNames = new List<string> { roleName },
                });
                var userId1 = response.UserId;

                // Create the target user
                loginId2 = Guid.NewGuid().ToString();
                response = await _descopeClient.Management.User.Create(loginId: loginId2, new UserRequest()
                {
                    Phone = "+972666666666",
                });
                var userId2 = response.UserId;

                // Have user1 impersonate user2
                var jwt = await _descopeClient.Management.Jwt.Impersonate(userId1, loginId2);

                // Make sure the session is valid
                var token = await _descopeClient.Auth.ValidateSession(jwt);
                Assert.Equal(userId2, token.Id);
            }
            finally
            {
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Management.Role.Delete(roleName); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(loginId2))
                {
                    try { await _descopeClient.Management.User.Delete(loginId2); }
                    catch { }
                }
            }
        }
    }
}
