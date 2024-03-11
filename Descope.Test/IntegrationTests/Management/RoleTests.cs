using Xunit;

namespace Descope.Test.Integration
{
    public class RoleTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Role_CreateAndLoad()
        {
            string? name = null;
            try
            {
                // Create a role
                name = Guid.NewGuid().ToString();
                var desc = "desc";
                await _descopeClient.Management.Role.Create(name, desc);

                // Load and compare
                var loadedRoles = await _descopeClient.Management.Role.LoadAll();
                var loadedRole = loadedRoles.Find(role => role.Name == name);
                Assert.NotNull(loadedRole);
                Assert.Equal(loadedRole.Description, desc);
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Management.Role.Delete(name); }
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
                await _descopeClient.Management.Role.Create(name, desc);
                updatedName = name + "updated";

                // Update and compare
                await _descopeClient.Management.Role.Update(name, updatedName);
                // Load and compare
                var foundRoles = await _descopeClient.Management.Role.SearchAll(new RoleSearchOptions { RoleNames = new List<string> { updatedName } });
                var role = foundRoles.Find(role => role.Name == updatedName);
                Assert.NotNull(role);
                Assert.True(string.IsNullOrEmpty(role.Description));
                foundRoles = await _descopeClient.Management.Role.SearchAll(new RoleSearchOptions { RoleNames = new List<string> { name } });
                role = foundRoles.Find(role => role.Name == name);
                Assert.Null(role);
                name = null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Management.Role.Delete(name); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(updatedName))
                {
                    try { await _descopeClient.Management.Role.Delete(updatedName); }
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
                await _descopeClient.Management.Role.Create(name);

                // Delete it
                await _descopeClient.Management.Role.Delete(name);
                name = null;

                // Load all and make sure it's gone
                var loadedRoles = await _descopeClient.Management.Role.LoadAll();
                var loadedRole = loadedRoles.Find(role => role.Name == name);
                Assert.Null(loadedRole);
            }
            finally
            {
                if (!string.IsNullOrEmpty(name))
                {
                    try { await _descopeClient.Management.Role.Delete(name); }
                    catch { }
                }
            }
        }
    }
}
