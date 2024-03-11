using Xunit;

namespace Descope.Test.Integration
{
    public class PermissionTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Permission_CreateAndLoad()
        {
            string? name = null;
            try
            {
                // Create a permission
                name = Guid.NewGuid().ToString();
                var desc = "desc";
                await _descopeClient.Management.Permission.Create(name, desc);

                // Load and compare
                var loadedPermissions = await _descopeClient.Management.Permission.LoadAll();
                var loadedPermission = loadedPermissions.Find(permission => permission.Name == name);
                Assert.NotNull(loadedPermission);
                Assert.Equal(loadedPermission.Description, desc);
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Management.Permission.Delete(name); }
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
                await _descopeClient.Management.Permission.Create(name, desc);
                updatedName = name + "updated";

                // Update and compare
                await _descopeClient.Management.Permission.Update(name, updatedName);
                // Load and compare
                var loadedPermissions = await _descopeClient.Management.Permission.LoadAll();
                var loadedPermission = loadedPermissions.Find(permission => permission.Name == updatedName);
                var originalNamePermission = loadedPermissions.Find(permission => permission.Name == name);
                Assert.Null(originalNamePermission);
                Assert.NotNull(loadedPermission);
                Assert.True(string.IsNullOrEmpty(loadedPermission.Description));
                name = null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Management.Permission.Delete(name); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(updatedName))
                {
                    try { await _descopeClient.Management.Permission.Delete(updatedName); }
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
                await _descopeClient.Management.Permission.Create(name);

                // Delete it
                await _descopeClient.Management.Permission.Delete(name);
                name = null;

                // Load all and make sure it's gone
                var loadedPermissions = await _descopeClient.Management.Permission.LoadAll();
                var loadedPermission = loadedPermissions.Find(permission => permission.Name == name);
                Assert.Null(loadedPermission);
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Management.Permission.Delete(name); }
                    catch { }
                }
            }
        }
    }

}
