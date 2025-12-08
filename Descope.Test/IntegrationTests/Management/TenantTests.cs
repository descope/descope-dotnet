using Xunit;

namespace Descope.Test.Integration
{
    public class TenantTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        // Create a three-tier tenant hierarchy (parent → child → grandchild) and assert Parent and Successor relationships between tenants
        [Fact]
        public async Task Tenant_CreateAndLoad()
        {
            string? parentTenantId = null;
            string? childTenantId = null;
            string? grandchildTenantId = null;
            try
            {
                // Create parent tenant
                var parentName = Guid.NewGuid().ToString();
                var parentDomain = parentName + ".com";
                var parentOptions = new TenantOptions(parentName)
                {
                    SelfProvisioningDomains = new List<string> { parentDomain },
                };
                parentTenantId = await _descopeClient.Management.Tenant.Create(options: parentOptions);

                // Create child tenant with parent relation
                var childName = Guid.NewGuid().ToString();
                var childOptions = new TenantOptions(childName)
                {
                    Parent = parentTenantId,
                };
                childTenantId = await _descopeClient.Management.Tenant.Create(options: childOptions);

                // Create grandchild tenant with child as parent
                var grandchildName = Guid.NewGuid().ToString();
                var grandchildOptions = new TenantOptions(grandchildName)
                {
                    Parent = childTenantId,
                };
                grandchildTenantId = await _descopeClient.Management.Tenant.Create(options: grandchildOptions);

                // Load parent and verify successors
                var loadedParent = await _descopeClient.Management.Tenant.LoadById(parentTenantId);
                Assert.Equal(loadedParent.Name, parentOptions.Name);
                Assert.NotNull(loadedParent.SelfProvisioningDomains);
                Assert.Contains(parentDomain, loadedParent.SelfProvisioningDomains);
                Assert.Empty(loadedParent.Parent);
                Assert.NotNull(loadedParent.Successors);
                Assert.Contains(childTenantId, loadedParent.Successors);

                // Load child and verify parent and successors
                var loadedChild = await _descopeClient.Management.Tenant.LoadById(childTenantId);
                Assert.Equal(loadedChild.Name, childOptions.Name);
                Assert.Equal(parentTenantId, loadedChild.Parent);
                Assert.NotNull(loadedChild.Successors);
                Assert.Contains(grandchildTenantId, loadedChild.Successors);

                // Load grandchild and verify parent
                var loadedGrandchild = await _descopeClient.Management.Tenant.LoadById(grandchildTenantId);
                Assert.Equal(loadedGrandchild.Name, grandchildOptions.Name);
                Assert.Equal(childTenantId, loadedGrandchild.Parent);
                Assert.NotNull(loadedGrandchild.Successors);
                Assert.Empty(loadedGrandchild.Successors);
            }
            finally
            {
                // Cleanup in reverse order (child before parent)
                if (!string.IsNullOrEmpty(grandchildTenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(grandchildTenantId); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(childTenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(childTenantId); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(parentTenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(parentTenantId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Tenant_Create_MissingName()
        {
            async Task Act() => await _descopeClient.Management.Tenant.Create(new TenantOptions(""));
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("Tenant name is required", result.Message);
        }

        [Fact]
        public async Task Tenant_UpdateAndSearch()
        {
            string? tenantId = null;
            try
            {
                // Create a tenant
                string tenantName = Guid.NewGuid().ToString();
                tenantId = await _descopeClient.Management.Tenant.Create(options: new TenantOptions(tenantName));
                var updatedTenantName = tenantName + "updated";

                // Update and compare
                await _descopeClient.Management.Tenant.Update(tenantId, new TenantOptions(updatedTenantName));
                var tenants = await _descopeClient.Management.Tenant.SearchAll(new TenantSearchOptions { Ids = new List<string> { tenantId } });
                Assert.Single(tenants);
                Assert.Equal(tenants[0].Name, updatedTenantName);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Tenant_Update_MissingId()
        {
            async Task Act() => await _descopeClient.Management.Tenant.Update("", new TenantOptions(""));
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("Tenant ID is required", result.Message);
        }

        [Fact]
        public async Task Tenant_Update_MissingName()
        {
            async Task Act() => await _descopeClient.Management.Tenant.Update("someId", new TenantOptions(""));
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("name cannot be updated to empty", result.Message);
        }

        [Fact]
        public async Task Tenant_DeleteAndLoadAll()
        {
            string? tenantId = null;
            try
            {
                // Create a tenant
                var id = await _descopeClient.Management.Tenant.Create(options: new TenantOptions(Guid.NewGuid().ToString()));
                tenantId = id;

                // Delete it
                await _descopeClient.Management.Tenant.Delete(id);
                tenantId = null;

                // Load all and make sure it's gone
                var tenants = await _descopeClient.Management.Tenant.LoadAll();
                foreach (var tenant in tenants)
                {
                    Assert.NotEqual(id, tenant.Id);
                }
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Tenant_Delete_MissingId()
        {
            async Task Act() => await _descopeClient.Management.Tenant.Delete("");
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("Tenant ID is required", result.Message);
        }
    }

}
