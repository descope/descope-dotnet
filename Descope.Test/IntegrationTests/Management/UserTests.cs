using Xunit;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Userv1;
using Descope.Mgmt.Models.Onetimev1;

namespace Descope.Test.Integration
{
    public class UserTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task User_Create()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Email = name + "@test.com",
                };
                var result = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = result?.User?.LoginIds?.FirstOrDefault();

                Assert.NotNull(result?.User);
                Assert.NotNull(loginId);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_CreateBatch()
        {
            List<string>? loginIds = null;
            try
            {
                // Prepare batch info
                var user1 = Guid.NewGuid().ToString();
                var user2 = Guid.NewGuid().ToString();
                var user3 = Guid.NewGuid().ToString();
                var user4 = Guid.NewGuid().ToString();
                var batchUsers = new List<CreateUsers>
                {
                    new CreateUsers
                    {
                        LoginId = user1,
                        Email = user1 + "@test.com",
                        VerifiedEmail = true,
                        Status = EnumValues.UserStatus.Enabled,
                    },
                    new CreateUsers
                    {
                        LoginId = user2,
                        Email = user2 + "@test.com",
                        VerifiedEmail = false,
                        Status = EnumValues.UserStatus.Disabled,
                    },
                    new CreateUsers
                    {
                        LoginId = user3,
                        Email = user3 + "@test.com",
                        VerifiedEmail = true,
                        Status = EnumValues.UserStatus.Invited,
                    },
                    new CreateUsers
                    {
                        LoginId = user4,
                        Email = user4 + "@test.com",
                        VerifiedEmail = true,
                        // No Status set - testing backwards compatibility
                    }
                };

                // Create batch and check
                var batchRequest = new CreateUsersRequest
                {
                    Users = batchUsers
                };
                var result = await _descopeClient.Mgmt.V1.User.Create.Batch.PostAsync(batchRequest);
                Assert.NotNull(result?.CreatedUsers);
                Assert.True(result.CreatedUsers.Count == 4);

                loginIds = new List<string>();
                foreach (var createdUser in result.CreatedUsers)
                {
                    var loginId = createdUser.LoginIds?.FirstOrDefault();
                    if (loginId != null)
                    {
                        loginIds.Add(loginId);
                        if (loginId == user1)
                        {
                            Assert.True(createdUser.VerifiedEmail);
                            Assert.Equal(EnumValues.UserStatus.Enabled, createdUser.Status);
                        }
                        else if (loginId == user2)
                        {
                            Assert.False(createdUser.VerifiedEmail);
                            Assert.Equal(EnumValues.UserStatus.Disabled, createdUser.Status);
                        }
                        else if (loginId == user3)
                        {
                            Assert.True(createdUser.VerifiedEmail);
                            Assert.Equal(EnumValues.UserStatus.Invited, createdUser.Status);
                        }
                        else if (loginId == user4)
                        {
                            Assert.True(createdUser.VerifiedEmail);
                            // User4 has no Status set - should get default behavior, no need to assert on it
                        }
                    }
                }
            }
            finally
            {
                // Cleanup
                if (loginIds != null)
                {
                    foreach (var loginId in loginIds)
                    {
                        try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                        catch { }
                    }
                }
            }
        }

        [Fact]
        public async Task User_Update()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                    GivenName = "a",
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                Assert.Equal("a", createResult?.User?.GivenName);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Update it
                var updateRequest = new UpdateUserRequest
                {
                    Identifier = loginId,
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                    GivenName = "b",
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.PostAsync(updateRequest);
                Assert.Equal("b", updateResult?.User?.GivenName);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_Activate()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                Assert.Equal(EnumValues.UserStatus.Invited, createResult?.User?.Status);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Deactivate
                var deactivateRequest = new UpdateUserStatusRequest
                {
                    Identifier = loginId,
                    Status = EnumValues.UserStatus.Disabled
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Status.PostAsync(deactivateRequest);
                Assert.Equal(EnumValues.UserStatus.Disabled, updateResult?.User?.Status);

                // Activate
                var activateRequest = new UpdateUserStatusRequest
                {
                    Identifier = loginId,
                    Status = EnumValues.UserStatus.Enabled
                };
                updateResult = await _descopeClient.Mgmt.V1.User.Update.Status.PostAsync(activateRequest);
                Assert.Equal(EnumValues.UserStatus.Enabled, updateResult?.User?.Status);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_UpdateLoginId()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Update login ID
                var updatedLoginId = Guid.NewGuid().ToString();
                var updateRequest = new UpdateUserLoginIDRequest
                {
                    LoginId = loginId,
                    NewLoginId = updatedLoginId
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Loginid.PostAsync(updateRequest);
                loginId = updatedLoginId;

                // Assert
                Assert.Equal(updatedLoginId, updateResult?.User?.LoginIds?.FirstOrDefault());
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_UpdateEmail()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Update email
                var updatedEmail = Guid.NewGuid().ToString() + "@test.com";
                var updateRequest = new UpdateUserEmailRequest
                {
                    Identifier = loginId,
                    Email = updatedEmail,
                    Verified = true
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Email.PostAsync(updateRequest);

                // Assert
                Assert.Equal(updatedEmail, updateResult?.User?.Email);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_UpdatePhone()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Update phone
                var updatedPhone = "+972555555556";
                var updateRequest = new UpdateUserPhoneRequest
                {
                    Identifier = loginId,
                    Phone = updatedPhone,
                    Verified = true
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Phone.PostAsync(updateRequest);

                // Assert
                Assert.Equal(updatedPhone, updateResult?.User?.Phone);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_UpdateDisplayName()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    Name = "a"
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Update display name
                var updateRequest = new UpdateUserDisplayNameRequest
                {
                    Identifier = loginId,
                    Name = "b"
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Name.PostAsync(updateRequest);

                // Assert
                Assert.Equal("b", updateResult?.User?.Name);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_UpdateUserNames()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    GivenName = "a",
                    MiddleName = "a",
                    FamilyName = "a",
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Update user names (using UpdateUserDisplayNameRequest which supports all name fields)
                var updateRequest = new UpdateUserDisplayNameRequest
                {
                    Identifier = loginId,
                    GivenName = "b",
                    MiddleName = "b",
                    FamilyName = "b"
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Name.PostAsync(updateRequest);

                // Assert
                Assert.Equal("b", updateResult?.User?.GivenName);
                Assert.Equal("b", updateResult?.User?.MiddleName);
                Assert.Equal("b", updateResult?.User?.FamilyName);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_UpdatePicture()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    Picture = "https://pics.com/a",
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Update picture
                var updateRequest = new UpdateUserPictureRequest
                {
                    LoginId = loginId,
                    Picture = "https://pics.com/b"
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Picture.PostAsync(updateRequest);

                // Assert
                Assert.Equal("https://pics.com/b", updateResult?.User?.Picture);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact(Skip = "Requires the custom attribute 'a' to exist on the project")]
        public async Task User_UpdateCustomAttributes()
        {
            string? loginId = null;
            try
            {
                // Create a user with custom attributes
                var name = Guid.NewGuid().ToString();
                var customAttributes = new CreateUserRequest_customAttributes();
                customAttributes.AdditionalData = new Dictionary<string, object> { { "a", "b" } };

                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    CustomAttributes = customAttributes,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Update custom attribute - using AttributeKey and value as object
                var updateRequest = new UpdateUserCustomAttributeRequest
                {
                    LoginId = loginId,
                    AttributeKey = "a"
                };
                // Set AttributeValue through AdditionalData since it's UntypedNode
                updateRequest.AdditionalData["attributeValue"] = "c";
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.CustomAttribute.PostAsync(updateRequest);
                Assert.NotNull(updateResult?.User?.CustomAttributes);
                Assert.NotNull(updateResult.User.CustomAttributes.AdditionalData);
                Assert.Equal("c", updateResult.User.CustomAttributes.AdditionalData["a"].ToString());
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_Roles()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Add roles
                var roleNames = new List<string> { "Tenant Admin" };
                var addRequest = new UpdateUserRolesRequest
                {
                    Identifier = loginId,
                    RoleNames = roleNames
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Role.Add.PostAsync(addRequest);
                Assert.NotNull(updateResult?.User?.RoleNames);
                Assert.Single(updateResult.User.RoleNames);
                Assert.Contains("Tenant Admin", updateResult.User.RoleNames);

                // Remove roles
                var removeRequest = new UpdateUserRolesRequest
                {
                    Identifier = loginId,
                    RoleNames = roleNames
                };
                updateResult = await _descopeClient.Mgmt.V1.User.Update.Role.Remove.PostAsync(removeRequest);
                Assert.NotNull(updateResult?.User?.RoleNames);
                Assert.Empty(updateResult.User.RoleNames);

                // Set roles
                var setRequest = new UpdateUserRolesRequest
                {
                    Identifier = loginId,
                    RoleNames = roleNames
                };
                updateResult = await _descopeClient.Mgmt.V1.User.Update.Role.Set.PostAsync(setRequest);
                Assert.NotNull(updateResult?.User?.RoleNames);
                Assert.Single(updateResult.User.RoleNames);
                Assert.Contains("Tenant Admin", updateResult.User.RoleNames);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_SsoApps()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Add SSO apps
                var ssoApps = new List<string> { "descope-default-oidc" };
                var addRequest = new UpdateUserSSOAppsRequest
                {
                    Identifier = loginId,
                    SsoAppIds = ssoApps
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Ssoapp.Add.PostAsync(addRequest);
                Assert.NotNull(updateResult?.User?.SsoAppIds);
                Assert.Single(updateResult.User.SsoAppIds);
                Assert.Contains("descope-default-oidc", updateResult.User.SsoAppIds);

                // Remove SSO apps
                var removeRequest = new UpdateUserSSOAppsRequest
                {
                    Identifier = loginId,
                    SsoAppIds = ssoApps
                };
                updateResult = await _descopeClient.Mgmt.V1.User.Update.Ssoapp.Remove.PostAsync(removeRequest);
                Assert.NotNull(updateResult?.User?.SsoAppIds);
                Assert.Empty(updateResult.User.SsoAppIds);

                // Set SSO apps
                var setRequest = new UpdateUserSSOAppsRequest
                {
                    Identifier = loginId,
                    SsoAppIds = ssoApps
                };
                updateResult = await _descopeClient.Mgmt.V1.User.Update.Ssoapp.Set.PostAsync(setRequest);
                Assert.NotNull(updateResult?.User?.SsoAppIds);
                Assert.Single(updateResult.User.SsoAppIds);
                Assert.Contains("descope-default-oidc", updateResult.User.SsoAppIds);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_Tenants()
        {
            string? loginId = null;
            string? tenantId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Create a tenant
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenantResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);
                tenantId = tenantResponse?.Id;

                // Add tenant
                var addRequest = new UpdateUserTenantRequest
                {
                    Identifier = loginId,
                    TenantId = tenantId
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Tenant.Add.PostAsync(addRequest);
                Assert.NotNull(updateResult?.User?.UserTenants);
                Assert.Single(updateResult.User.UserTenants);
                var t = updateResult.User.UserTenants.FirstOrDefault(t => t.TenantId == tenantId);
                Assert.NotNull(t);

                // Remove tenant
                var removeRequest = new UpdateUserTenantRequest
                {
                    Identifier = loginId,
                    TenantId = tenantId
                };
                updateResult = await _descopeClient.Mgmt.V1.User.Update.Tenant.Remove.PostAsync(removeRequest);
                Assert.NotNull(updateResult?.User?.UserTenants);
                Assert.Empty(updateResult.User.UserTenants);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_Password()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();
                Assert.False(createResult?.User?.Password);

                // Set an active password
                var setPasswordRequest = new SetUserPasswordRequest
                {
                    Identifier = loginId,
                    Password = "abCD123#$abCD123#$"
                };
                await _descopeClient.Mgmt.V1.User.Password.Set.PostAsync(setPasswordRequest);

                var loadResult = await _descopeClient.Mgmt.V1.User.LoadAsync(loginId!);
                Assert.True(loadResult?.User?.Password);

                // Expire password
                var expireRequest = new ExpireUserPasswordRequest
                {
                    Identifier = loginId
                };
                await _descopeClient.Mgmt.V1.User.Password.Expire.PostAsync(expireRequest);

                // Set temporary password (using Set endpoint)
                var setTempPasswordRequest = new SetUserPasswordRequest
                {
                    Identifier = loginId,
                    Password = "abCD123#$abCD123#$"
                };
                await _descopeClient.Mgmt.V1.User.Password.Set.PostAsync(setTempPasswordRequest);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_DeleteAndSearch()
        {
            string? loginId = null;
            string? tenantId = null;
            try
            {
                // Create a tenant and role
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenantResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);
                tenantId = tenantResponse?.Id;

                var roleName = "Tenant Admin";
                var createRoleRequest = new CreateRoleRequest
                {
                    Name = roleName,
                    TenantId = tenantId
                };
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(createRoleRequest);

                // Create a user with tenant and role
                var name = Guid.NewGuid().ToString();
                var userTenant = new AssociatedTenant
                {
                    TenantId = tenantId,
                    RoleNames = new List<string> { roleName }
                };
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972111111111",
                    VerifiedPhone = true,
                    UserTenants = new List<AssociatedTenant> { userTenant }
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Search for user by name
                var searchByNameRequest = new SearchUsersRequest
                {
                    Text = name,
                    Limit = 1
                };
#pragma warning disable CS0618 // Type or member is obsolete
                var users = await _descopeClient.Mgmt.V1.User.Search.PostAsync(searchByNameRequest);
#pragma warning restore CS0618
                Assert.NotNull(users?.Users);
                Assert.Single(users.Users);
                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByNameRequest);
                Assert.NotNull(users?.Users);
                Assert.Single(users.Users);

                var searchByTenantRoles = new SearchUsersRequest
                {
                    Limit = 10,
                    TenantIds = new List<string> { tenantId! },
                    RoleNames = new List<string> { roleName! }

                };
#pragma warning disable CS0618 // Type or member is obsolete
                users = await _descopeClient.Mgmt.V1.User.Search.PostAsync(searchByTenantRoles);
#pragma warning restore CS0618
                Assert.NotNull(users?.Users);
                Assert.Single(users.Users);
                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByTenantRoles);
                Assert.NotNull(users?.Users);
                Assert.Single(users.Users);

                // Delete user
                await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId });
                loginId = null;

                // Search again by name - should be empty
#pragma warning disable CS0618 // Type or member is obsolete
                users = await _descopeClient.Mgmt.V1.User.Search.PostAsync(searchByNameRequest);
#pragma warning restore CS0618
                Assert.NotNull(users?.Users);
                Assert.Empty(users.Users);
                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByNameRequest);
                Assert.NotNull(users?.Users);
                Assert.Empty(users.Users);

                // Search again by TenantRoleNames - should be empty
#pragma warning disable CS0618 // Type or member is obsolete
                users = await _descopeClient.Mgmt.V1.User.Search.PostAsync(searchByTenantRoles);
#pragma warning restore CS0618
                Assert.NotNull(users?.Users);
                Assert.Empty(users.Users);
                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByTenantRoles);
                Assert.NotNull(users?.Users);
                Assert.Empty(users.Users);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
            }
        }


        [Fact]
        public async Task User_TestUser()
        {
            string? loginId = null;
            try
            {
                // Create a test user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972111111111",
                    VerifiedPhone = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Generate OTP for test user
                var otpRequest = new TestUserGenerateOTPRequest
                {
                    LoginId = loginId,
                    DeliveryMethod = "email"
                };
                var otp = await _descopeClient.Mgmt.V1.Tests.Generate.Otp.PostAsync(otpRequest);
                Assert.Equal(loginId, otp?.LoginId);
                Assert.NotEmpty(otp?.Code!);

                // Generate magic link for test user
                var mlRequest = new TestUserGenerateMagicLinkRequest
                {
                    LoginId = loginId,
                    DeliveryMethod = "email"
                };
                var ml = await _descopeClient.Mgmt.V1.Tests.Generate.Magiclink.PostAsync(mlRequest);
                Assert.NotEmpty(ml?.Link!);
                Assert.Equal(loginId, ml?.LoginId);

                // Generate enchanted link for test user
                var elRequest = new TestUserGenerateEnchantedLinkRequest
                {
                    LoginId = loginId
                };
                var el = await _descopeClient.Mgmt.V1.Tests.Generate.Enchantedlink.PostAsync(elRequest);
                Assert.NotEmpty(el?.Link!);
                Assert.NotEmpty(el?.PendingRef!);
                Assert.Equal(loginId, el?.LoginId);

                // Note: Enable embedded authentication to test
                // var eml = await _descopeClient.Mgmt.V1.User.Signin.Embeddedlink.PostAsync(...);
                // Assert.NotEmpty(eml);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_GenerateEmbeddedLink_WithNonExistingUser_ShouldFail()
        {
            // Use a non-existing login ID
            var nonExistingLoginId = Guid.NewGuid().ToString() + "@nonexisting.com";

            // Try to generate embedded link for non-existing user
            async Task Act() => await _descopeClient.Mgmt.V1.User.Signin.Embeddedlink.PostAsync(new EmbeddedLinkSignInRequest
            {
                LoginId = nonExistingLoginId
            });

            // Should throw an exception for non-existing user
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.NotNull(exception);
            Assert.NotEmpty(exception.Message);
        }

        [Fact]
        public async Task User_LoadUser_ShouldDeserializeAllFields()
        {
            string? loginId = null;
            string? tenantId = null;
            try
            {
                // Create a tenant for testing
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenantResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);
                tenantId = tenantResponse?.Id;

                // Create a comprehensive user with all possible fields
                var uniqueName = Guid.NewGuid().ToString();
                var userEmail = uniqueName + "@test.com";
                var userPhone = "+972555555555";
                var customAttributes = new CreateUserRequest_customAttributes();
                customAttributes.AdditionalData = new Dictionary<string, object>
                {
                    // Note: custom attributes require the attribute to exist in the project
                    // { "customKey", "customValue" }
                };

                var userTenant = new AssociatedTenant
                {
                    TenantId = tenantId,
                    RoleNames = new List<string> { "Tenant Admin" }
                };

                var createRequest = new CreateUserRequest
                {
                    Identifier = uniqueName,
                    Email = userEmail,
                    VerifiedEmail = true,
                    Phone = userPhone,
                    VerifiedPhone = true,
                    Name = "Display Name",
                    GivenName = "Given",
                    MiddleName = "Middle",
                    FamilyName = "Family",
                    Picture = "https://example.com/picture.jpg",
                    CustomAttributes = customAttributes,
                    UserTenants = new List<AssociatedTenant> { userTenant },
                    RoleNames = new List<string> { "Tenant Admin" },
                    SsoAppIds = new List<string> { "descope-default-oidc" }
                };

                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();
                Assert.NotNull(loginId);

                // Load the user and verify all fields are correctly deserialized
                var loadResult = await _descopeClient.Mgmt.V1.User.LoadAsync(loginId!);

                Assert.NotNull(loadResult);
                Assert.NotNull(loadResult.User);

                var user = loadResult.User;

                // Verify login IDs
                Assert.NotNull(user.LoginIds);
                Assert.Single(user.LoginIds);
                Assert.Equal(uniqueName, user.LoginIds.First());

                // Verify user ID
                Assert.NotNull(user.UserId);
                Assert.NotEmpty(user.UserId);

                // Verify email fields
                Assert.Equal(userEmail, user.Email);
                Assert.True(user.VerifiedEmail);

                // Verify phone fields
                Assert.Equal(userPhone, user.Phone);
                Assert.True(user.VerifiedPhone);

                // Verify name fields
                Assert.Equal("Display Name", user.Name);
                Assert.Equal("Given", user.GivenName);
                Assert.Equal("Middle", user.MiddleName);
                Assert.Equal("Family", user.FamilyName);

                // Verify picture
                Assert.Equal("https://example.com/picture.jpg", user.Picture);

                // Verify status
                Assert.Equal(EnumValues.UserStatus.Invited, user.Status);

                // Verify test user flag
                Assert.False(user.Test);

                // Verify custom attributes (if any were set)
                Assert.NotNull(user.CustomAttributes);

                // Verify tenants
                Assert.NotNull(user.UserTenants);
                Assert.Single(user.UserTenants);
                var tenant = user.UserTenants.First();
                Assert.Equal(tenantId, tenant.TenantId);
                Assert.NotNull(tenant.RoleNames);
                Assert.Contains("Tenant Admin", tenant.RoleNames);

                // Verify roles
                Assert.NotNull(user.RoleNames);
                Assert.Contains("Tenant Admin", user.RoleNames);

                // Verify SSO apps
                Assert.NotNull(user.SsoAppIds);
                Assert.Contains("descope-default-oidc", user.SsoAppIds);

                // Verify created time
                Assert.NotEqual(0, user.CreatedTime);

                // Verify TOTP flag
                Assert.False(user.TOTP);

                // Verify SAML flag
                Assert.False(user.SAML);

                // Verify OAuth provider info
                Assert.NotNull(user.OAuth);

                // Verify password flag
                Assert.False(user.Password);

                // Verify WebAuthn flag
                Assert.False(user.Webauthn);

                // Verify push flag
                Assert.False(user.Push);

                // Verify SCIM flag
                Assert.False(user.SCIM);

                // Verify external IDs
                Assert.NotNull(user.ExternalIds);
                Assert.Single(user.ExternalIds);
                Assert.Equal(uniqueName, user.ExternalIds.First());

                // Verify permissions collection
                Assert.NotNull(user.Permissions);
                Assert.NotEmpty(user.Permissions);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
            }
        }
    }
}
