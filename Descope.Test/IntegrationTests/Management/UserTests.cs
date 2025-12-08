using Xunit;

namespace Descope.Test.Integration
{
    public class UserTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task User_Create()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var result = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Email = name + "@test.com",
                });
                loginId = result.LoginIds.First();
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var batchUsers = new List<BatchUser>()
                {
                    new(loginId: user1)
                    {
                        Email = user1 + "@test.com",
                        VerifiedEmail = true,
                    },
                    new(loginId: user2)
                    {
                        Email = user2 + "@test.com",
                        VerifiedEmail = false,
                    }
                };

                // Create batch and check
                var result = await _descopeClient.Management.User.CreateBatch(batchUsers);
                Assert.True(result.CreatedUsers.Count == 2);
                loginIds = new List<string>();
                foreach (var createdUser in result.CreatedUsers)
                {
                    var loginId = createdUser.LoginIds.First();
                    loginIds.Add(loginId);
                    if (loginId == user1)
                    {
                        Assert.True(createdUser.VerifiedEmail);
                    }
                    else if (loginId == user2)
                    {
                        Assert.False(createdUser.VerifiedEmail);
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
                        try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                    GivenName = "a",
                });
                Assert.Equal("a", createResult.GivenName);
                loginId = createResult.LoginIds.First();

                // Update it
                var updateResult = await _descopeClient.Management.User.Update(loginId, new UserRequest()
                {
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                    GivenName = "b",
                });
                Assert.Equal("b", updateResult.GivenName);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                    GivenName = "a",
                });
                Assert.Equal("a", createResult.GivenName);
                loginId = createResult.LoginIds.First();

                // Update it
                var patchResult = await _descopeClient.Management.User.Patch(loginId, new UserRequest()
                {
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                    GivenName = "b",
                });
                Assert.Equal("b", patchResult.GivenName);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                });
                Assert.Equal("invited", createResult.Status);
                loginId = createResult.LoginIds.First();

                // Act
                var updateResult = await _descopeClient.Management.User.Deactivate(loginId);
                Assert.Equal("disabled", updateResult.Status);
                updateResult = await _descopeClient.Management.User.Activate(loginId);
                Assert.Equal("enabled", updateResult.Status);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                });
                loginId = createResult.LoginIds.First();

                // Act
                var updatedLoginId = Guid.NewGuid().ToString();
                var updateResult = await _descopeClient.Management.User.UpdateLoginId(loginId, updatedLoginId);
                loginId = updatedLoginId;

                // Assert
                Assert.Equal(updatedLoginId, updateResult.LoginIds.First());
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Email = name + "@test.com",
                    VerifiedEmail = true,
                });
                loginId = createResult.LoginIds.First();

                // Act
                var updatedEmail = Guid.NewGuid().ToString() + "@test.com";
                var updateResult = await _descopeClient.Management.User.UpdateEmail(loginId, updatedEmail, true);

                // Assert
                Assert.Equal(updatedEmail, updateResult.Email);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                });
                loginId = createResult.LoginIds.First();

                // Act
                var updatedPhone = "+972555555556";
                var updateResult = await _descopeClient.Management.User.UpdatePhone(loginId, updatedPhone, true);

                // Assert
                Assert.Equal(updatedPhone, updateResult.Phone);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    Name = "a"
                });
                loginId = createResult.LoginIds.First();

                // Act
                var updateResult = await _descopeClient.Management.User.UpdateDisplayName(loginId, "b");

                // Assert
                Assert.Equal("b", updateResult.Name);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    GivenName = "a",
                    MiddleName = "a",
                    FamilyName = "a",
                });
                loginId = createResult.LoginIds.First();

                // Act
                var updateResult = await _descopeClient.Management.User.UpdateUserNames(loginId, "b", "b", "b");

                // Assert
                Assert.Equal("b", updateResult.GivenName);
                Assert.Equal("b", updateResult.MiddleName);
                Assert.Equal("b", updateResult.FamilyName);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    Picture = "https://pics.com/a",
                });
                loginId = createResult.LoginIds.First();

                // Act
                var updateResult = await _descopeClient.Management.User.UpdatePicture(loginId, "https://pics.com/b");

                // Assert
                Assert.Equal("https://pics.com/b", updateResult.Picture);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    CustomAttributes = new Dictionary<string, object> { { "a", "b" } },
                });
                loginId = createResult.LoginIds.First();

                // Update custom attribute
                var updateResult = await _descopeClient.Management.User.UpdateCustomAttributes(loginId, "a", "c");
                Assert.NotNull(updateResult.CustomAttributes);
                Assert.Equal("c", updateResult.CustomAttributes["a"].ToString());
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                });
                loginId = createResult.LoginIds.First();

                // Check add roles
                var roleNames = new List<string> { "Tenant Admin" };
                var updateResult = await _descopeClient.Management.User.AddRoles(loginId, roleNames);
                Assert.NotNull(updateResult.RoleNames);
                Assert.Single(updateResult.RoleNames);
                Assert.Contains("Tenant Admin", updateResult.RoleNames);

                // Check remove roles
                updateResult = await _descopeClient.Management.User.RemoveRoles(loginId, roleNames);
                Assert.NotNull(updateResult.RoleNames);
                Assert.Empty(updateResult.RoleNames);

                // Check set roles
                updateResult = await _descopeClient.Management.User.SetRoles(loginId, roleNames);
                Assert.NotNull(updateResult.RoleNames);
                Assert.Single(updateResult.RoleNames);
                Assert.Contains("Tenant Admin", updateResult.RoleNames);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                });
                loginId = createResult.LoginIds.First();

                // Check add sso apps
                var ssoApps = new List<string> { "descope-default-oidc" };
                var updateResult = await _descopeClient.Management.User.AddSsoApps(loginId, ssoApps);
                Assert.NotNull(updateResult.SsoAppIds);
                Assert.Single(updateResult.SsoAppIds);
                Assert.Contains("descope-default-oidc", updateResult.SsoAppIds);

                // Check remove sso apps
                updateResult = await _descopeClient.Management.User.RemoveSsoApps(loginId, ssoApps);
                Assert.NotNull(updateResult.SsoAppIds);
                Assert.Empty(updateResult.SsoAppIds);

                // Check set sso apps
                updateResult = await _descopeClient.Management.User.SetSsoApps(loginId, ssoApps);
                Assert.NotNull(updateResult.SsoAppIds);
                Assert.Single(updateResult.SsoAppIds);
                Assert.Contains("descope-default-oidc", updateResult.SsoAppIds);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                });
                loginId = createResult.LoginIds.First();

                // Create a tenant
                tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions(Guid.NewGuid().ToString()));

                // Check add roles
                var updateResult = await _descopeClient.Management.User.AddTenant(loginId, tenantId);
                Assert.NotNull(updateResult.UserTenants);
                Assert.Single(updateResult.UserTenants);
                var t = updateResult.UserTenants.Find(t => t.TenantId == tenantId);
                Assert.NotNull(t);

                // Check remove roles
                updateResult = await _descopeClient.Management.User.RemoveTenant(loginId, tenantId);
                Assert.NotNull(updateResult.UserTenants);
                Assert.Empty(updateResult.UserTenants);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                });
                loginId = createResult.LoginIds.First();
                Assert.False(createResult.Password);

                // Set a temporary password
                await _descopeClient.Management.User.SetActivePassword(loginId, "abCD123#$abCD123#$");
                var loadResult = await _descopeClient.Management.User.Load(loginId);
                Assert.True(loadResult.Password);
                await _descopeClient.Management.User.ExpirePassword(loginId);
                await _descopeClient.Management.User.SetTemporaryPassword(loginId, "abCD123#$abCD123#$");
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task User_DeleteAndSearch()
        {
            string? loginId = null;
            try
            {
                // Create a user
                var name = Guid.NewGuid().ToString();
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972111111111",
                    VerifiedPhone = true,
                });
                loginId = createResult.LoginIds.First();

                // Search for it
                var users = await _descopeClient.Management.User.SearchAll(new SearchUserOptions() { Text = name, Limit = 1 });
                Assert.Single(users);
                await _descopeClient.Management.User.Delete(loginId);
                loginId = null;
                users = await _descopeClient.Management.User.SearchAll(new SearchUserOptions { Text = name, Limit = 1 });
                Assert.Empty(users);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var createResult = await _descopeClient.Management.User.Create(loginId: name, new UserRequest()
                {
                    Phone = "+972111111111",
                    VerifiedPhone = true,

                }, testUser: true);
                loginId = createResult.LoginIds.First();

                // Generate all manor of auth
                var otp = await _descopeClient.Management.User.GenerateOtpForTestUser(DeliveryMethod.Email, loginId);
                Assert.Equal(loginId, otp.LoginId);
                Assert.NotEmpty(otp.Code);
                var ml = await _descopeClient.Management.User.GenerateMagicLinkForTestUser(DeliveryMethod.Email, loginId);
                Assert.NotEmpty(ml.Link);
                Assert.Equal(loginId, ml.LoginId);
                var el = await _descopeClient.Management.User.GenerateEnchantedLinkForTestUser(loginId);
                Assert.NotEmpty(el.Link);
                Assert.NotEmpty(el.PendingRef);
                Assert.Equal(loginId, el.LoginId);
                // Note: Enable embedded authentication to test
                // var eml = await _descopeClient.Management.User.GenerateEmbeddedLink(loginId);
                // Assert.NotEmpty(eml);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
            }
        }

    }
}
