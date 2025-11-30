using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    public class JwtTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Jwt_CustomClaims()
        {
            string? loginId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.User?.LoginIds?.FirstOrDefault();

                // Update JWT with custom claims
                var customClaims = new UpdateJWTRequest_customClaims();
                customClaims.AdditionalData["a"] = "b";

                var updateJwtRequest = new UpdateJWTRequest
                {
                    Jwt = testUser.AuthInfo.SessionJwt,
                    CustomClaims = customClaims
                };

                var updateJwtResponse = await _descopeClient.Mgmt.V1.Jwt.Update.PostAsync(updateJwtRequest);
                var updatedJwt = updateJwtResponse?.Jwt;
                Assert.NotNull(updatedJwt);

                // Make sure the session is valid and has the custom claims
                var token = await _descopeClient.Auth.ValidateSession(updatedJwt!);
                Assert.Contains("a", token.Claims.Keys);
                Assert.Equal("b", token.Claims["a"].ToString());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
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
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(new CreateRoleRequest
                {
                    Name = roleName,
                    PermissionNames = new List<string> { "Impersonate" }
                });

                // Create impersonating user
                loginId = Guid.NewGuid().ToString();
                var createUserRequest1 = new CreateUserRequest
                {
                    Identifier = loginId,
                    Phone = "+972555555555",
                    RoleNames = new List<string> { roleName }
                };
                var response = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest1);
                var userId1 = response?.User?.UserId;
                Assert.NotNull(userId1);

                // Create the target user
                loginId2 = Guid.NewGuid().ToString();
                var createUserRequest2 = new CreateUserRequest
                {
                    Identifier = loginId2,
                    Phone = "+972666666666"
                };
                var response2 = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest2);
                var userId2 = response2?.User?.UserId;
                Assert.NotNull(userId2);

                // Have user1 impersonate user2
                var impersonateRequest = new ImpersonateRequest
                {
                    ImpersonatorId = userId1,
                    LoginId = loginId2
                };
                var impersonateResponse = await _descopeClient.Mgmt.V1.Impersonate.PostAsync(impersonateRequest);
                var jwt = impersonateResponse?.Jwt;
                Assert.NotNull(jwt);

                // Validate the impersonation data
                var token = await _descopeClient.Auth.ValidateSession(jwt!);
                Assert.Equal(userId2, token.Id);
                Assert.Contains(userId1!, token.Claims["act"].ToString()!);
            }
            finally
            {
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = roleName }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(loginId2))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId2 }); }
                    catch { }
                }
            }
        }
    }
}
