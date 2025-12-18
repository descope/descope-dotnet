using Xunit;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Onetimev1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class UserTests : RateLimitedIntegrationTest
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
                // Create a user with initial values
                var uniqueName = Guid.NewGuid().ToString();
                var initialEmail = uniqueName + "@test.com";
                var initialPhone = "+972555555555";
                var customAttributes = new CreateUserRequest_customAttributes();
                customAttributes.AdditionalData = new Dictionary<string, object>();

                var createRequest = new CreateUserRequest
                {
                    Identifier = uniqueName,
                    Email = initialEmail,
                    VerifiedEmail = true,
                    Phone = initialPhone,
                    VerifiedPhone = false,
                    Name = "Initial Display Name",
                    GivenName = "Initial Given",
                    MiddleName = "Initial Middle",
                    FamilyName = "Initial Family",
                    Picture = "https://example.com/initial.jpg",
                    CustomAttributes = customAttributes,
                    RoleNames = new List<string> { "Tenant Admin" },
                    SsoAppIds = new List<string> { "descope-default-oidc" }
                };

                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();
                Assert.NotNull(loginId);

                // Verify initial values
                Assert.Equal("Initial Given", createResult?.User?.GivenName);
                Assert.Equal("Initial Middle", createResult?.User?.MiddleName);
                Assert.Equal("Initial Family", createResult?.User?.FamilyName);
                Assert.Equal("Initial Display Name", createResult?.User?.Name);
                Assert.Equal("https://example.com/initial.jpg", createResult?.User?.Picture);
                Assert.True(createResult?.User?.VerifiedEmail);
                Assert.False(createResult?.User?.VerifiedPhone);

                // Update user with new values for all updatable fields
                var updatedEmail = uniqueName + "_updated@test.com";
                var updatedPhone = "+972555555556";
                var updateCustomAttributes = new UpdateUserRequest_customAttributes();
                updateCustomAttributes.AdditionalData = new Dictionary<string, object>();

                var updateRequest = new UpdateUserRequest
                {
                    Identifier = loginId,
                    Email = updatedEmail,
                    VerifiedEmail = false,
                    Phone = updatedPhone,
                    VerifiedPhone = true,
                    Name = "Updated Display Name",
                    GivenName = "Updated Given",
                    MiddleName = "Updated Middle",
                    FamilyName = "Updated Family",
                    Picture = "https://example.com/updated.jpg",
                    CustomAttributes = updateCustomAttributes,
                    RoleNames = new List<string> { "Tenant Admin" },
                    SsoAppIds = new List<string> { "descope-default-oidc" }
                };

                var updateResult = await _descopeClient.Mgmt.V1.User.Update.PostAsync(updateRequest);

                // Verify update response
                Assert.NotNull(updateResult?.User);
                Assert.Equal("Updated Given", updateResult.User.GivenName);
                Assert.Equal("Updated Middle", updateResult.User.MiddleName);
                Assert.Equal("Updated Family", updateResult.User.FamilyName);
                Assert.Equal("Updated Display Name", updateResult.User.Name);
                Assert.Equal("https://example.com/updated.jpg", updateResult.User.Picture);
                Assert.Equal(updatedEmail, updateResult.User.Email);
                Assert.False(updateResult.User.VerifiedEmail);
                Assert.Equal(updatedPhone, updateResult.User.Phone);
                Assert.True(updateResult.User.VerifiedPhone);

                // Reload the user and verify all changes persisted
                var loadResult = await _descopeClient.Mgmt.V1.User.GetWithIdentifierAsync(loginId!);

                Assert.NotNull(loadResult);
                Assert.NotNull(loadResult.User);

                var user = loadResult.User;

                // Verify all updated fields
                Assert.Equal("Updated Given", user.GivenName);
                Assert.Equal("Updated Middle", user.MiddleName);
                Assert.Equal("Updated Family", user.FamilyName);
                Assert.Equal("Updated Display Name", user.Name);
                Assert.Equal("https://example.com/updated.jpg", user.Picture);
                Assert.Equal(updatedEmail, user.Email);
                Assert.False(user.VerifiedEmail);
                Assert.Equal(updatedPhone, user.Phone);
                Assert.True(user.VerifiedPhone);

                // Verify login IDs
                Assert.NotNull(user.LoginIds);
                Assert.Single(user.LoginIds);
                Assert.Equal(uniqueName, user.LoginIds.First());

                // Verify user ID is still the same
                Assert.NotNull(user.UserId);
                Assert.Equal(createResult?.User?.UserId, user.UserId);

                // Verify roles
                Assert.NotNull(user.RoleNames);
                Assert.Contains("Tenant Admin", user.RoleNames);

                // Verify SSO apps
                Assert.NotNull(user.SsoAppIds);
                Assert.Contains("descope-default-oidc", user.SsoAppIds);

                // Verify custom attributes
                Assert.NotNull(user.CustomAttributes);
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
        public async Task User_Patch()
        {
            string? loginId = null;
            try
            {
                // Create a user with initial values
                var uniqueName = Guid.NewGuid().ToString();
                var initialEmail = uniqueName + "@test.com";
                var initialPhone = "+972555555555";
                var customAttributes = new CreateUserRequest_customAttributes();
                customAttributes.AdditionalData = new Dictionary<string, object>();

                var createRequest = new CreateUserRequest
                {
                    Identifier = uniqueName,
                    Email = initialEmail,
                    VerifiedEmail = true,
                    Phone = initialPhone,
                    VerifiedPhone = false,
                    Name = "Initial Display Name",
                    GivenName = "Initial Given",
                    MiddleName = "Initial Middle",
                    FamilyName = "Initial Family",
                    Picture = "https://example.com/initial.jpg",
                    CustomAttributes = customAttributes,
                    RoleNames = new List<string> { "Tenant Admin" },
                    SsoAppIds = new List<string> { "descope-default-oidc" }
                };

                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();
                Assert.NotNull(loginId);

                // Verify initial values
                Assert.Equal("Initial Given", createResult?.User?.GivenName);
                Assert.Equal("Initial Middle", createResult?.User?.MiddleName);
                Assert.Equal("Initial Family", createResult?.User?.FamilyName);
                Assert.Equal("Initial Display Name", createResult?.User?.Name);
                Assert.Equal("https://example.com/initial.jpg", createResult?.User?.Picture);
                Assert.True(createResult?.User?.VerifiedEmail);
                Assert.False(createResult?.User?.VerifiedPhone);

                // Patch user with partial update (only update some fields)
                var updatedEmail = uniqueName + "_updated@test.com";
                var patchCustomAttributes = new PatchUserRequest_customAttributes();
                patchCustomAttributes.AdditionalData = new Dictionary<string, object>();

                var patchRequest = new PatchUserRequest
                {
                    Identifier = loginId,
                    Email = updatedEmail,
                    VerifiedEmail = true, // Keep verified to maintain valid user state
                    GivenName = "Patched Given",
                    Picture = "https://example.com/patched.jpg",
                    CustomAttributes = patchCustomAttributes
                };

                var patchResult = await _descopeClient.Mgmt.V1.User.PatchPath.PatchAsync(patchRequest);

                // Verify patch response - updated fields
                Assert.NotNull(patchResult?.User);
                Assert.Equal("Patched Given", patchResult.User.GivenName);
                Assert.Equal("https://example.com/patched.jpg", patchResult.User.Picture);
                Assert.Equal(updatedEmail, patchResult.User.Email);
                Assert.True(patchResult.User.VerifiedEmail);

                // Verify patch response - unchanged fields should remain
                Assert.Equal("Initial Middle", patchResult.User.MiddleName);
                Assert.Equal("Initial Family", patchResult.User.FamilyName);
                Assert.Equal(initialPhone, patchResult.User.Phone);
                Assert.False(patchResult.User.VerifiedPhone);

                // Reload the user and verify all changes persisted
                var loadResult = await _descopeClient.Mgmt.V1.User.GetWithIdentifierAsync(loginId!);

                Assert.NotNull(loadResult);
                Assert.NotNull(loadResult.User);

                var user = loadResult.User;

                // Verify patched fields
                Assert.Equal("Patched Given", user.GivenName);
                Assert.Equal("https://example.com/patched.jpg", user.Picture);
                Assert.Equal(updatedEmail, user.Email);
                Assert.True(user.VerifiedEmail);

                // Verify unchanged fields
                Assert.Equal("Initial Middle", user.MiddleName);
                Assert.Equal("Initial Family", user.FamilyName);
                Assert.Equal(initialPhone, user.Phone);
                Assert.False(user.VerifiedPhone);

                // Verify login IDs
                Assert.NotNull(user.LoginIds);
                Assert.Single(user.LoginIds);
                Assert.Equal(uniqueName, user.LoginIds.First());

                // Verify user ID is still the same
                Assert.NotNull(user.UserId);
                Assert.Equal(createResult?.User?.UserId, user.UserId);

                // Verify roles (should be unchanged)
                Assert.NotNull(user.RoleNames);
                Assert.Contains("Tenant Admin", user.RoleNames);

                // Verify SSO apps (should be unchanged)
                Assert.NotNull(user.SsoAppIds);
                Assert.Contains("descope-default-oidc", user.SsoAppIds);

                // Verify custom attributes
                Assert.NotNull(user.CustomAttributes);
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
        public async Task User_UpdateWithTenants()
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

                // Create a user with initial tenant
                var uniqueName = Guid.NewGuid().ToString();
                var initialTenant = new AssociatedTenant
                {
                    TenantId = tenantId,
                    RoleNames = new List<string> { "Tenant Admin" }
                };

                var createRequest = new CreateUserRequest
                {
                    Identifier = uniqueName,
                    Email = uniqueName + "@test.com",
                    VerifiedEmail = true,
                    UserTenants = new List<AssociatedTenant> { initialTenant }
                };

                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();
                Assert.NotNull(loginId);

                // Verify initial tenant
                Assert.NotNull(createResult?.User?.UserTenants);
                Assert.Single(createResult.User.UserTenants);
                Assert.Equal(tenantId, createResult.User.UserTenants.First().TenantId);

                // Update user tenants (note: cannot include roleNames when userTenants is set)
                var updatedTenant = new AssociatedTenant
                {
                    TenantId = tenantId,
                    RoleNames = new List<string> { "Tenant Admin" }
                };

                var updateRequest = new UpdateUserRequest
                {
                    Identifier = loginId,
                    Email = uniqueName + "@test.com",
                    UserTenants = new List<AssociatedTenant> { updatedTenant }
                };

                var updateResult = await _descopeClient.Mgmt.V1.User.Update.PostAsync(updateRequest);

                // Verify update response
                Assert.NotNull(updateResult?.User);
                Assert.NotNull(updateResult.User.UserTenants);
                Assert.Single(updateResult.User.UserTenants);

                // Reload the user and verify tenant persisted
                var loadResult = await _descopeClient.Mgmt.V1.User.GetWithIdentifierAsync(loginId!);

                Assert.NotNull(loadResult);
                Assert.NotNull(loadResult.User);

                var user = loadResult.User;

                // Verify tenants
                Assert.NotNull(user.UserTenants);
                Assert.Single(user.UserTenants);
                var tenant = user.UserTenants.First();
                Assert.Equal(tenantId, tenant.TenantId);
                Assert.NotNull(tenant.RoleNames);
                Assert.Contains("Tenant Admin", tenant.RoleNames);
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
            string? permissionName = null;
            string? roleName = null;
            string? tenantId = null;
            string? tenantPermissionName = null;
            string? tenantRoleName = null;
            try
            {
                // Create a unique permission for this test (project-level)
                permissionName = "TestPermission_" + Guid.NewGuid().ToString();
                var createPermissionRequest = new CreatePermissionRequest
                {
                    Name = permissionName,
                    Description = "Test permission for role testing"
                };
                await _descopeClient.Mgmt.V1.Permission.Create.PostAsync(createPermissionRequest);

                // Create a unique role with the permission (project-level)
                roleName = "TestRole_" + Guid.NewGuid().ToString();
                var createRoleRequest = new CreateRoleRequest
                {
                    Name = roleName,
                    Description = "Test role with permission",
                    PermissionNames = new List<string> { permissionName }
                };
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(createRoleRequest);

                // Create a tenant for tenant-specific roles
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var tenantResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);
                tenantId = tenantResponse?.Id;

                // Create a tenant-specific permission
                tenantPermissionName = "TestTenantPermission_" + Guid.NewGuid().ToString();
                var createTenantPermissionRequest = new CreatePermissionRequest
                {
                    Name = tenantPermissionName,
                    Description = "Test tenant-specific permission",
                };
                await _descopeClient.Mgmt.V1.Permission.Create.PostAsync(createTenantPermissionRequest);

                // Create a tenant-specific role with the permission
                tenantRoleName = "TestTenantRole_" + Guid.NewGuid().ToString();
                var createTenantRoleRequest = new CreateRoleRequest
                {
                    Name = tenantRoleName,
                    Description = "Test tenant-specific role with permission",
                    PermissionNames = new List<string> { tenantPermissionName },
                    TenantId = tenantId
                };
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(createTenantRoleRequest);

                // Create a test user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();

                // Verify the roles were created with the permissions
                await RetryUntilSuccessAsync(async () =>
                {
                    var roleResponse = await _descopeClient.Mgmt.V1.Role.All.GetAsync();
                    var createdRole = roleResponse?.Roles?.Find(r => r.Name == roleName);
                    Assert.NotNull(createdRole);
                    Assert.NotNull(createdRole.PermissionNames);
                    Assert.Contains(permissionName, createdRole.PermissionNames);

                    var createdTenantRole = roleResponse?.Roles?.Find(r => r.Name == tenantRoleName);
                    Assert.NotNull(createdTenantRole);
                    Assert.NotNull(createdTenantRole.PermissionNames);
                    Assert.Contains(tenantPermissionName, createdTenantRole.PermissionNames);
                    Assert.Equal(tenantId, createdTenantRole.TenantId);
                });

                // Add the project-level role to the user
                var roleNames = new List<string> { roleName };
                var addRequest = new UpdateUserRolesRequest
                {
                    Identifier = loginId,
                    RoleNames = roleNames
                };
                var updateResult = await _descopeClient.Mgmt.V1.User.Update.Role.Add.PostAsync(addRequest);
                Assert.NotNull(updateResult?.User?.RoleNames);
                Assert.Single(updateResult.User.RoleNames);
                Assert.Contains(roleName, updateResult.User.RoleNames);

                // Add the tenant to the user (without roles first)
                var addTenantRequest = new UpdateUserTenantRequest
                {
                    Identifier = loginId,
                    TenantId = tenantId
                };
                await _descopeClient.Mgmt.V1.User.Update.Tenant.Add.PostAsync(addTenantRequest);

                // Add the tenant-specific role to the user
                var tenantRoleRequest = new UpdateUserRolesRequest
                {
                    Identifier = loginId,
                    RoleNames = new List<string> { tenantRoleName },
                    TenantId = tenantId
                };
                var tenantUpdateResult = await _descopeClient.Mgmt.V1.User.Update.Role.Add.PostAsync(tenantRoleRequest);

                // Verify tenant-specific role was added
                await RetryUntilSuccessAsync(async () =>
                {
                    var loadResult = await _descopeClient.Mgmt.V1.User.GetWithIdentifierAsync(loginId!);
                    Assert.NotNull(loadResult?.User?.UserTenants);
                    Assert.Single(loadResult.User.UserTenants);
                    var userTenant = loadResult.User.UserTenants.FirstOrDefault(t => t.TenantId == tenantId);
                    Assert.NotNull(userTenant);
                    Assert.NotNull(userTenant.RoleNames);
                    Assert.Contains(tenantRoleName, userTenant.RoleNames);
                });

                // Get a valid user session (JWT) and check it for roles and permissions
                await RetryUntilSuccessAsync(async () =>
                {
                    // Generate a fresh token to get latest role information
                    var magicLinkRequest = new Descope.Mgmt.Models.Onetimev1.TestUserGenerateMagicLinkRequest
                    {
                        DeliveryMethod = "email",
                        LoginId = loginId,
                        RedirectUrl = "https://example.com/auth"
                    };
                    var magicLinkResponse = await _descopeClient.Mgmt.V1.Tests.Generate.Magiclink.PostAsync(magicLinkRequest);
                    var uri = new Uri(magicLinkResponse!.Link!); // magic link url with token is returned for test users
                    var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var magicLinkToken = queryParams["t"];
                    var verifyRequest = new Descope.Auth.Models.Onetimev1.VerifyMagicLinkRequest
                    {
                        Token = magicLinkToken
                    };
                    var authResponse = await _descopeClient.Auth.V1.Magiclink.Verify.PostAsync(verifyRequest);

                    // Select the tenant to get tenant-specific claims in the token
                    var selectTenantRequest = new Descope.Auth.Models.Onetimev1.SelectTenantRequest
                    {
                        Tenant = tenantId
                    };
                    var tenantSession = await _descopeClient.Auth.V1.Tenant.Select.PostWithJwtAsync(
                        selectTenantRequest,
                        authResponse!.RefreshJwt!);

                    var sessionToken = await _descopeClient.Auth.ValidateSessionAsync(tenantSession!.SessionJwt!);

                    // Verify the tenant is in the token
                    var userTenants = sessionToken.GetTenants();
                    Assert.Contains(tenantId, userTenants);

                    // Validate that the token has the custom project-level role
                    Assert.True(sessionToken.ValidateRoles(new List<string> { roleName }));
                    // Validate roles with non-existent role returns false
                    Assert.False(sessionToken.ValidateRoles(new List<string> { "NonExistentRole" }));

                    // Get matched project-level roles - should return the custom role
                    var matchedRoles = sessionToken.GetMatchedRoles(new List<string> { roleName, "Other Role" });
                    Assert.Single(matchedRoles);
                    Assert.Contains(roleName, matchedRoles);

                    // Get MatchedRoles with non-existent role returns empty
                    var noRoleMatches = sessionToken.GetMatchedRoles(new List<string> { "NonExistentRole" });
                    Assert.Empty(noRoleMatches);

                    // Validate that the token has the test project-level permission
                    Assert.True(sessionToken.ValidatePermissions(new List<string> { permissionName }));
                    // Validate permissions doesn't match non-existent permission
                    Assert.False(sessionToken.ValidatePermissions(new List<string> { "NonExistentPermission" }));

                    // Get matched project-level permissions - should return only the test permission
                    var matchedPermissions = sessionToken.GetMatchedPermissions(new List<string> { permissionName, "NonExistentPermission" });
                    Assert.Single(matchedPermissions);
                    Assert.Contains(permissionName, matchedPermissions);

                    // GetMatchedPermissions with non-existent permission returns empty
                    var noMatches = sessionToken.GetMatchedPermissions(new List<string> { "NonExistentPermission" });
                    Assert.Empty(noMatches);

                    // Validate tenant-specific roles and permissions
                    Assert.NotNull(tenantId);

                    // Validate that the token has the tenant-specific role
                    Assert.True(sessionToken.ValidateRoles(new List<string> { tenantRoleName }, tenantId));
                    // Validate tenant roles with non-existent role returns false
                    Assert.False(sessionToken.ValidateRoles(new List<string> { "NonExistentTenantRole" }, tenantId));
                    // Validate tenant roles with non-existent tenant returns false
                    Assert.False(sessionToken.ValidateRoles(new List<string> { tenantRoleName }, "NonExistentTenant"));

                    // Get matched tenant-specific roles - should return the tenant role
                    var matchedTenantRoles = sessionToken.GetMatchedRoles(new List<string> { tenantRoleName, "Other Tenant Role" }, tenantId);
                    Assert.Single(matchedTenantRoles);
                    Assert.Contains(tenantRoleName, matchedTenantRoles);

                    // Get matched roles with non-existent tenant returns empty
                    var noTenantRoleMatches = sessionToken.GetMatchedRoles(new List<string> { tenantRoleName }, "NonExistentTenant");
                    Assert.Empty(noTenantRoleMatches);

                    // Validate that the token has the tenant-specific permission
                    Assert.True(sessionToken.ValidatePermissions(new List<string> { tenantPermissionName }, tenantId));
                    // Validate tenant permissions with non-existent permission returns false
                    Assert.False(sessionToken.ValidatePermissions(new List<string> { "NonExistentTenantPermission" }, tenantId));
                    // Validate tenant permissions with non-existent tenant returns false
                    Assert.False(sessionToken.ValidatePermissions(new List<string> { tenantPermissionName }, "NonExistentTenant"));

                    // Get matched tenant-specific permissions - should return only the tenant permission
                    var matchedTenantPermissions = sessionToken.GetMatchedPermissions(new List<string> { tenantPermissionName, "NonExistentTenantPermission" }, tenantId);
                    Assert.Single(matchedTenantPermissions);
                    Assert.Contains(tenantPermissionName, matchedTenantPermissions);

                    // GetMatchedPermissions with non-existent tenant returns empty
                    var noTenantMatches = sessionToken.GetMatchedPermissions(new List<string> { tenantPermissionName }, "NonExistentTenant");
                    Assert.Empty(noTenantMatches);
                });

                // Remove roles
                var removeRequest = new UpdateUserRolesRequest
                {
                    Identifier = loginId,
                    RoleNames = roleNames,
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
                Assert.Contains(roleName, updateResult.User.RoleNames);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(roleName))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = roleName }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(permissionName))
                {
                    try { await _descopeClient.Mgmt.V1.Permission.DeletePath.PostAsync(new DeletePermissionRequest { Name = permissionName }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(tenantRoleName))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = tenantRoleName }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(tenantPermissionName))
                {
                    try { await _descopeClient.Mgmt.V1.Permission.DeletePath.PostAsync(new DeletePermissionRequest { Name = tenantPermissionName }); }
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
                    Password = "abCD123#$abCD123#$",
                };
                await _descopeClient.Mgmt.V1.User.Password.Set.PostAsync(setPasswordRequest);

                var loadResult = await _descopeClient.Mgmt.V1.User.GetWithIdentifierAsync(loginId!);
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
                var users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByNameRequest);
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
                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByTenantRoles);
                Assert.NotNull(users?.Users);
                Assert.Single(users.Users);
                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByTenantRoles);
                Assert.NotNull(users?.Users);
                Assert.Single(users.Users);

                // Delete user
                await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId });
                loginId = null;

                // Search again by name - should be empty
                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByNameRequest);
                Assert.NotNull(users?.Users);
                Assert.Empty(users.Users);
                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByNameRequest);
                Assert.NotNull(users?.Users);
                Assert.Empty(users.Users);

                users = await _descopeClient.Mgmt.V2.User.Search.PostAsync(searchByTenantRoles);
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
                var loadResult = await _descopeClient.Mgmt.V1.User.GetWithIdentifierAsync(loginId!);

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

        [Fact]
        public async Task User_AuthHistory()
        {
            string? loginId = null;
            string? userId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createRequest = new CreateUserRequest
                {
                    Identifier = name,
                    Email = name + "@test.com",
                };
                var createResult = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createRequest);
                loginId = createResult?.User?.LoginIds?.FirstOrDefault();
                userId = createResult?.User?.UserId;

                Assert.NotNull(loginId);
                Assert.NotNull(userId);

                // Load auth history for the user (newly created user won't have history, but API should work)
                await RetryUntilSuccessAsync(async () =>
                {
                    var historyRequest = new UsersAuthHistoryRequest
                    {
                        UserIds = new List<string> { userId }
                    };
                    var historyResult = await _descopeClient.Mgmt.V2.User.History.PostAsync(historyRequest);

                    // Response should not be null, but history list may be empty for a new user
                    Assert.NotNull(historyResult);
                    Assert.NotNull(historyResult.UsersAuthHistory);
                });
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
    }
}
