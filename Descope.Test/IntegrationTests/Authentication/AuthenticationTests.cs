using Xunit;
using Descope.Mgmt.Models.Managementv1;
using Descope.Auth.Models.Onetimev1;

namespace Descope.Test.Integration
{
    public class AuthenticationTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Authentication_ValidateAndRefresh()
        {
            string? loginId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.User?.LoginIds?.FirstOrDefault();

                // Make sure the session is valid
                var token = await _descopeClient.Auth.ValidateSessionAsync(testUser.AuthInfo.SessionJwt!);
                Assert.NotNull(token.Jwt);
                Assert.Equal(testUser.AuthInfo.SessionJwt, token.Jwt);
                Assert.NotEmpty(token.Id);
                Assert.NotEmpty(token.Subject);
                Assert.NotEmpty(token.ProjectId);

                // Refresh and see we got a new token
                var refreshedToken = await _descopeClient.Auth.RefreshSessionAsync(testUser.AuthInfo.RefreshJwt!);
                Assert.NotNull(refreshedToken.RefreshExpiration);
                Assert.Equal(token.Id, refreshedToken.Id);
                Assert.Equal(token.Subject, refreshedToken.Subject);
                Assert.Equal(token.ProjectId, refreshedToken.ProjectId);
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
        public async Task Authentication_ExchangeAccessKeyAndMe()
        {
            string? loginId = null;
            string? accessKeyId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.User?.LoginIds?.FirstOrDefault();

                // Create an access key and exchange it
                var createAccessKeyRequest = new CreateAccessKeyRequest
                {
                    Name = "Integration Test Key",
                    UserId = testUser.User.User?.UserId
                };
                var accessKeyResponse = await _descopeClient.Mgmt.V1.Accesskey.Create.PostAsync(createAccessKeyRequest);
                accessKeyId = accessKeyResponse?.Key?.Id;
                var token = await _descopeClient.Auth.ExchangeAccessKey(accessKeyResponse?.Cleartext!);
                Assert.NotEmpty(token.Subject);
                Assert.NotEmpty(token.Id);
                Assert.NotEmpty(token.ProjectId);
                Assert.Contains(IntegrationTestSetup.ProjectId!, token.ProjectId);
                Assert.NotEmpty(token.Jwt);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(accessKeyId))
                {
                    try { await _descopeClient.Mgmt.V1.Accesskey.DeletePath.PostAsync(new AccessKeyRequest { Id = accessKeyId }); }
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
                loginId = testUser.User.User?.LoginIds?.FirstOrDefault();

                // Create a couple of tenants and add to the user
                var createTenantRequest1 = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenant1Response = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest1);
                var tenantId1 = tenant1Response?.Id!;
                tenantIds.Add(tenantId1);
                await _descopeClient.Mgmt.V1.User.Update.Tenant.Add.PostAsync(new UpdateUserTenantRequest
                {
                    Identifier = loginId,
                    TenantId = tenantId1
                });

                var createTenantRequest2 = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenant2Response = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest2);
                var tenantId2 = tenant2Response?.Id!;
                tenantIds.Add(tenantId2);
                await _descopeClient.Mgmt.V1.User.Update.Tenant.Add.PostAsync(new UpdateUserTenantRequest
                {
                    Identifier = loginId,
                    TenantId = tenantId2
                });

                // Select tenant using PostWithJwtAsync
                var selectTenantRequest = new SelectTenantRequest
                {
                    Tenant = tenantId2
                };
                var session = await _descopeClient.Auth.V1.Tenant.Select.PostWithJwtAsync(
                    selectTenantRequest,
                    testUser.AuthInfo.RefreshJwt!);

                Assert.NotNull(session);
                Assert.NotEmpty(session.SessionJwt!);
                Assert.NotEmpty(session.RefreshJwt!);

                var token = await _descopeClient.Auth.ValidateSessionAsync(session.SessionJwt!);
                Assert.NotEmpty(token.Id);
                Assert.NotEmpty(token.ProjectId);
                Assert.NotEmpty(token.Jwt);
                Assert.Equal(session.SessionJwt, token.Jwt);
                Assert.Contains(tenantId2, token.GetTenants());
                Assert.Equal(tenantId2, token.CurrentTenant);

            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
                foreach (var tenantId in tenantIds)
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
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
                loginId = testUser.User.User?.LoginIds?.FirstOrDefault();

                // Me
                var user = await _descopeClient.Auth.V1.Me.GetWithJwtAsync(testUser.AuthInfo.RefreshJwt!);
                Assert.Equal(testUser.User.User?.UserId, user?.UserId);

                // Logout
                var logoutRequest = new LogoutRequest();
                await _descopeClient.Auth.V1.Logout.PostWithJwtAsync(logoutRequest, testUser.AuthInfo.RefreshJwt!);

                // Try me again - should fail
                async Task Act() => await _descopeClient.Auth.V1.Me.GetWithJwtAsync(testUser.AuthInfo.RefreshJwt!);
                var result = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("Expired due to logout", result.Message);
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
    }
}
