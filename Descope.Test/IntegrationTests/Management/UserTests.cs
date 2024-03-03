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
                    email = name + "@test.com",
                });
                loginId = result.loginIds.First();
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
                    new()
                    {
                        loginId = user1,
                        email = user1 + "@test.com",
                        verifiedEmail = true,
                    },
                    new()
                    {
                        loginId = user2,
                        email = user2 + "@test.com",
                        verifiedEmail = false,
                    }
                };

                // Create batch and check
                var result = await _descopeClient.Management.User.CreateBatch(batchUsers);
                Assert.True(result.CreatedUsers.Count == 2);
                loginIds = new List<string>();
                foreach (var createdUser in result.CreatedUsers)
                {
                    var loginId = createdUser.loginIds.First();
                    loginIds.Add(loginId);
                    if (loginId == user1)
                    {
                        Assert.True(createdUser.verifiedEmail);
                    }
                    else if (loginId == user2)
                    {
                        Assert.False(createdUser.verifiedEmail);
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
                    email = name + "@test.com",
                    verifiedEmail = true,
                    givenName = "a",
                });
                Assert.Equal("a", createResult.givenName);
                loginId = createResult.loginIds.First();

                // Update it
                var updateResult = await _descopeClient.Management.User.Update(loginId, new UserRequest()
                {
                    email = name + "@test.com",
                    verifiedEmail = true,
                    givenName = "b",
                });
                Assert.Equal("b", updateResult.givenName);
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
                    email = name + "@test.com",
                    verifiedEmail = true,
                });
                Assert.Equal("invited", createResult.status);
                loginId = createResult.loginIds.First();

                // Act
                var updateResult = await _descopeClient.Management.User.Deactivate(loginId);
                Assert.Equal("disabled", updateResult.status);
                updateResult = await _descopeClient.Management.User.Activate(loginId);
                Assert.Equal("enabled", updateResult.status);
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
                    email = name + "@test.com",
                    verifiedEmail = true,
                });
                loginId = createResult.loginIds.First();

                // Act
                var updatedLoginId = Guid.NewGuid().ToString();
                var updateResult = await _descopeClient.Management.User.UpdateLoginId(loginId, updatedLoginId);
                loginId = updatedLoginId;

                // Assert
                Assert.Equal(updatedLoginId, updateResult.loginIds.First());
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
                    email = name + "@test.com",
                    verifiedEmail = true,
                });
                loginId = createResult.loginIds.First();

                // Act
                var updatedEmail = Guid.NewGuid().ToString() + "@test.com";
                var updateResult = await _descopeClient.Management.User.UpdateEmail(loginId, updatedEmail, true);

                // Assert
                Assert.Equal(updatedEmail, updateResult.email);
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
                    phone = "+972555555555",
                    verifiedPhone = true,
                });
                loginId = createResult.loginIds.First();

                // Act
                var updatedPhone = "+972555555556";
                var updateResult = await _descopeClient.Management.User.UpdatePhone(loginId, updatedPhone, true);

                // Assert
                Assert.Equal(updatedPhone, updateResult.phone);
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
                    phone = "+972555555555",
                    name = "a"
                });
                loginId = createResult.loginIds.First();

                // Act
                var updateResult = await _descopeClient.Management.User.UpdateDisplayName(loginId, "b");

                // Assert
                Assert.Equal("b", updateResult.name);
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
                    phone = "+972555555555",
                    givenName = "a",
                    middleName = "a",
                    familyName = "a",
                });
                loginId = createResult.loginIds.First();

                // Act
                var updateResult = await _descopeClient.Management.User.UpdateUserNames(loginId, "b", "b", "b");

                // Assert
                Assert.Equal("b", updateResult.givenName);
                Assert.Equal("b", updateResult.middleName);
                Assert.Equal("b", updateResult.familyName);
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
                    phone = "+972555555555",
                    picture = "https://pics.com/a",
                });
                loginId = createResult.loginIds.First();

                // Act
                var updateResult = await _descopeClient.Management.User.UpdatePicture(loginId, "https://pics.com/b");

                // Assert
                Assert.Equal("https://pics.com/b", updateResult.picture);
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
                    phone = "+972555555555",
                    customAttributes = new Dictionary<string, object> { { "a", "b" } },
                });
                loginId = createResult.loginIds.First();

                // Update custom attribute
                var updateResult = await _descopeClient.Management.User.UpdateCustomAttributes(loginId, "a", "c");
                Assert.Equal("c", updateResult.customAttributes["a"].ToString());
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
                    phone = "+972555555555",
                    verifiedPhone = true,
                });
                loginId = createResult.loginIds.First();

                // Check add roles
                var roleNames = new List<string> { "Tenant Admin" };
                var updateResult = await _descopeClient.Management.User.AddRoles(loginId, roleNames);
                Assert.Single(updateResult.roleNames);
                Assert.Contains("Tenant Admin", updateResult.roleNames);

                // Check remove roles
                updateResult = await _descopeClient.Management.User.RemoveRoles(loginId, roleNames);
                Assert.Empty(updateResult.roleNames);

                // Check set roles
                updateResult = await _descopeClient.Management.User.SetRoles(loginId, roleNames);
                Assert.Single(updateResult.roleNames);
                Assert.Contains("Tenant Admin", updateResult.roleNames);
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
                    phone = "+972555555555",
                    verifiedPhone = true,
                });
                loginId = createResult.loginIds.First();

                // Check add sso apps
                var ssoApps = new List<string> { "descope-default-oidc" };
                var updateResult = await _descopeClient.Management.User.AddSsoApps(loginId, ssoApps);
                Assert.Single(updateResult.ssoAppIds);
                Assert.Contains("descope-default-oidc", updateResult.ssoAppIds);

                // Check remove sso apps
                updateResult = await _descopeClient.Management.User.RemoveSsoApps(loginId, ssoApps);
                Assert.Empty(updateResult.ssoAppIds);

                // Check set sso apps
                updateResult = await _descopeClient.Management.User.SetSsoApps(loginId, ssoApps);
                Assert.Single(updateResult.ssoAppIds);
                Assert.Contains("descope-default-oidc", updateResult.ssoAppIds);
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
                    phone = "+972555555555",
                    verifiedPhone = true,
                });
                loginId = createResult.loginIds.First();

                // Create a tenant
                tenantId = await _descopeClient.Management.Tenant.Create(new TenantOptions { name = Guid.NewGuid().ToString() });

                // Check add roles
                var updateResult = await _descopeClient.Management.User.AddTenant(loginId, tenantId);
                Assert.Single(updateResult.userTenants);
                var t = updateResult.userTenants.Find(t => t.tenantId == tenantId);
                Assert.NotNull(t);

                // Check remove roles
                updateResult = await _descopeClient.Management.User.RemoveTenant(loginId, tenantId);
                Assert.Empty(updateResult.userTenants);
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
                    phone = "+972555555555",
                    verifiedPhone = true,
                });
                loginId = createResult.loginIds.First();
                Assert.False(createResult.password);

                // Set a temporary password
                await _descopeClient.Management.User.SetActivePassword(loginId, "abCD123#$");
                var loadResult = await _descopeClient.Management.User.Load(loginId);
                Assert.True(loadResult.password);
                await _descopeClient.Management.User.ExpirePassword(loginId);
                await _descopeClient.Management.User.SetTemporaryPassword(loginId, "abCD123#$");
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
                    phone = "+972111111111",
                    verifiedPhone = true,
                });
                loginId = createResult.loginIds.First();

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
                    phone = "+972111111111",
                    verifiedPhone = true,

                }, testUser: true);
                loginId = createResult.loginIds.First();

                // Generate all manor of auth
                var otp = await _descopeClient.Management.User.GenerateOtpForTestUser(DeliveryMethod.email, loginId);
                Assert.Equal(loginId, otp.LoginId);
                Assert.NotEmpty(otp.Code);
                var ml = await _descopeClient.Management.User.GenerateMagicLinkForTestUser(DeliveryMethod.email, loginId);
                Assert.NotEmpty(ml.Link);
                Assert.Equal(loginId, ml.LoginId);
                var el = await _descopeClient.Management.User.GenerateEnchantedLinkForTestUser(loginId);
                Assert.NotEmpty(el.Link);
                Assert.NotEmpty(el.PendingRef);
                Assert.Equal(loginId, el.LoginId);
                // TODO: Enable embedded authentication to test
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
