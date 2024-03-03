using Xunit;

namespace Descope.Test.Integration
{
    public class TenantTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Tenant_CreateAndLoad()
        {
            string? tenantId = null;
            try
            {
                // Create a tenant
                var name = Guid.NewGuid().ToString();
                var domain = name + ".com";
                var options = new TenantOptions
                {
                    name = name,
                    selfProvisioningDomains = new List<string> { domain },
                };
                tenantId = await _descopeClient.Management.Tenant.Create(options: options);

                // Load and compare
                var loadedTenant = await _descopeClient.Management.Tenant.LoadById(tenantId);
                Assert.Equal(loadedTenant.name, options.name);
                Assert.Contains(domain, loadedTenant.selfProvisioningDomains);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Management.Tenant.Delete(tenantId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Tenant_Create_MissingName()
        {
            async Task Act() => await _descopeClient.Management.Tenant.Create(new TenantOptions());
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
                tenantId = await _descopeClient.Management.Tenant.Create(options: new TenantOptions { name = tenantName });
                var updatedTenantName = tenantName + "updated";

                // Update and compare
                await _descopeClient.Management.Tenant.Update(tenantId, new TenantOptions { name = updatedTenantName });
                var tenants = await _descopeClient.Management.Tenant.SearchAll(new TenantSearchOptions { ids = new List<string> { tenantId } });
                Assert.Single(tenants);
                Assert.Equal(tenants[0].name, updatedTenantName);
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
            async Task Act() => await _descopeClient.Management.Tenant.Update("", new TenantOptions());
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("Tenant ID is required", result.Message);
        }

        [Fact]
        public async Task Tenant_Update_MissingName()
        {
            async Task Act() => await _descopeClient.Management.Tenant.Update("someId", new TenantOptions());
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
                var id = await _descopeClient.Management.Tenant.Create(options: new TenantOptions { name = Guid.NewGuid().ToString() });
                tenantId = id;

                // Delete it
                await _descopeClient.Management.Tenant.Delete(id);
                tenantId = null;

                // Load all and make sure it's gone
                var tenants = await _descopeClient.Management.Tenant.LoadAll();
                foreach (TenantResponse tenant in tenants)
                {
                    Assert.NotEqual(id, tenant.id);
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
