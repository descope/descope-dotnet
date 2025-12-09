using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class PermissionTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Permission_CreateAndLoad()
        {
            string? name = null;
            try
            {
                // Create a permission
                name = Guid.NewGuid().ToString();
                var desc = "desc";
                var createRequest = new CreatePermissionRequest
                {
                    Name = name,
                    Description = desc
                };
                await _descopeClient.Mgmt.V1.Permission.Create.PostAsync(createRequest);

                // Load and compare
                var loadedPermissionsResponse = await _descopeClient.Mgmt.V1.Permission.All.GetAsync();
                var loadedPermission = loadedPermissionsResponse?.Permissions?.Find(permission => permission.Name == name);
                Assert.NotNull(loadedPermission);
                Assert.Equal(desc, loadedPermission.Description);
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Permission.DeletePath.PostAsync(new DeletePermissionRequest { Name = name });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Permission_UpdateAndSearch()
        {
            string? name = null;
            string? updatedName = null;
            try
            {
                // Create a permission
                name = Guid.NewGuid().ToString();
                string desc = "desc";
                var createRequest = new CreatePermissionRequest
                {
                    Name = name,
                    Description = desc
                };
                await _descopeClient.Mgmt.V1.Permission.Create.PostAsync(createRequest);
                updatedName = name + "updated";

                // Update and compare
                var updateRequest = new UpdatePermissionRequest
                {
                    Name = name,
                    NewName = updatedName
                };
                await _descopeClient.Mgmt.V1.Permission.Update.PostAsync(updateRequest);

                // Load and compare
                var loadedPermissionsResponse = await _descopeClient.Mgmt.V1.Permission.All.GetAsync();
                var loadedPermission = loadedPermissionsResponse?.Permissions?.Find(permission => permission.Name == updatedName);
                var originalNamePermission = loadedPermissionsResponse?.Permissions?.Find(permission => permission.Name == name);
                Assert.Null(originalNamePermission);
                Assert.NotNull(loadedPermission);
                Assert.True(string.IsNullOrEmpty(loadedPermission.Description));
                name = null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Permission.DeletePath.PostAsync(new DeletePermissionRequest { Name = name });
                    }
                    catch { }
                }
                if (!string.IsNullOrEmpty(updatedName))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Permission.DeletePath.PostAsync(new DeletePermissionRequest { Name = updatedName });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Permission_DeleteAndLoadAll()
        {
            string? name = null;
            try
            {
                // Create a permission
                name = Guid.NewGuid().ToString();
                var createRequest = new CreatePermissionRequest
                {
                    Name = name
                };
                await _descopeClient.Mgmt.V1.Permission.Create.PostAsync(createRequest);

                // Delete it
                await _descopeClient.Mgmt.V1.Permission.DeletePath.PostAsync(new DeletePermissionRequest { Name = name });
                name = null;

                // Load all and make sure it's gone
                var loadedPermissionsResponse = await _descopeClient.Mgmt.V1.Permission.All.GetAsync();
                var loadedPermission = loadedPermissionsResponse?.Permissions?.Find(permission => permission.Name == name);
                Assert.Null(loadedPermission);
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Permission.DeletePath.PostAsync(new DeletePermissionRequest { Name = name });
                    }
                    catch { }
                }
            }
        }
    }
}
