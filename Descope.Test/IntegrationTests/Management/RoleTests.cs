using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class RoleTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        private async Task RetryUntilSuccessAsync(Func<Task> assertion, int timeoutSeconds = 6)
        {
            var endTime = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            Exception? lastException = null;

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    await assertion();
                    return; // Success!
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    await Task.Delay(100); // Wait 100ms before retry
                }
            }

            // If we get here, all retries failed
            throw lastException ?? new TimeoutException($"Assertion failed after {timeoutSeconds} seconds");
        }

        [Fact]
        public async Task Role_CreateAndLoad()
        {
            string? name = null;
            try
            {
                // Create a role
                name = Guid.NewGuid().ToString();
                var desc = "desc";
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(new CreateRoleRequest
                {
                    Name = name,
                    Description = desc
                });

                // Load and compare
                var loadedRolesResponse = await _descopeClient.Mgmt.V1.Role.All.GetAsync();
                var loadedRole = loadedRolesResponse?.Roles?.Find(role => role.Name == name);
                Assert.NotNull(loadedRole);
                Assert.Equal(desc, loadedRole.Description);
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = name }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Role_UpdateAndSearch()
        {
            string? name = null;
            string? updatedName = null;
            try
            {
                // Create a role
                name = Guid.NewGuid().ToString();
                string desc = "desc";
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(new CreateRoleRequest
                {
                    Name = name,
                    Description = desc
                });
                updatedName = name + "updated";

                // Update and compare
                await _descopeClient.Mgmt.V1.Role.Update.PostAsync(new UpdateRoleRequest
                {
                    Name = name,
                    NewName = updatedName
                });

                // Search for updated role
                await RetryUntilSuccessAsync(async () =>
                {
                    var foundRolesResponse = await _descopeClient.Mgmt.V1.Role.Search.PostAsync(new SearchRolesRequest
                    {
                        RoleNames = new List<string> { updatedName }
                    });
                    var role = foundRolesResponse?.Roles?.Find(r => r.Name == updatedName);
                    Assert.NotNull(role);
                    Assert.True(string.IsNullOrEmpty(role.Description));
                });

                // Search for old name - should not be found
                await RetryUntilSuccessAsync(async () =>
                {
                    var foundRolesResponse = await _descopeClient.Mgmt.V1.Role.Search.PostAsync(new SearchRolesRequest
                    {
                        RoleNames = new List<string> { name }
                    });
                    var role = foundRolesResponse?.Roles?.Find(r => r.Name == name);
                    Assert.Null(role);
                });
                name = null;

                // Load all and make sure only updated role is there
                await RetryUntilSuccessAsync(async () =>
                {
                    var loadedRolesResponse = await _descopeClient.Mgmt.V1.Role.All.GetAsync();
                    Assert.NotNull(loadedRolesResponse?.Roles);
                    var role = loadedRolesResponse?.Roles?.Find(r => r.Name == updatedName);
                    Assert.NotNull(role);
                    role = loadedRolesResponse?.Roles?.Find(r => r.Name == name);
                    Assert.Null(role);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = name }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(updatedName))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = updatedName }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Role_DeleteAndLoadAll()
        {
            string? name = null;
            try
            {
                // Create a role
                name = Guid.NewGuid().ToString();
                await _descopeClient.Mgmt.V1.Role.Create.PostAsync(new CreateRoleRequest
                {
                    Name = name
                });

                // Delete it
                await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = name });
                name = null;

                // Load all and make sure it's gone
                var loadedRolesResponse = await _descopeClient.Mgmt.V1.Role.All.GetAsync();
                var loadedRole = loadedRolesResponse?.Roles?.Find(role => role.Name == name);
                Assert.Null(loadedRole);
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Mgmt.V1.Role.DeletePath.PostAsync(new DeleteRoleRequest { Name = name }); }
                    catch { }
                }
            }
        }
    }
}
