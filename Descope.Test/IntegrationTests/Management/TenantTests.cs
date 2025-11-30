using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    public class TenantTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        // Create tenants and test load functionality
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
                var parentOptions = new CreateTenantRequest
                {
                    Name = parentName,
                    SelfProvisioningDomains = new List<string> { parentDomain }
                };
                var tenant1Response = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(parentOptions);
                parentTenantId = tenant1Response?.Id;
                Assert.NotNull(parentTenantId);

                // Create child tenant with parent relation
                var childName = Guid.NewGuid().ToString();
                var tenant2Response = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(new CreateTenantRequest
                {
                    Name = childName,
                    Parent = parentTenantId
                });
                childTenantId = tenant2Response?.Id;
                Assert.NotNull(childTenantId);

                // Create third tenant
                var grandchildName = Guid.NewGuid().ToString();
                var tenant3Response = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(new CreateTenantRequest
                {
                    Name = grandchildName,
                    Parent = childTenantId
                });
                grandchildTenantId = tenant3Response?.Id;
                Assert.NotNull(grandchildTenantId);

                // Load parent and verify successors
                var loadedParent = await _descopeClient.Mgmt.V1.Tenant.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Id = parentTenantId;
                });
                Assert.NotNull(loadedParent);
                Assert.NotNull(loadedParent.Tenant);
                Assert.Equal(parentName, loadedParent.Tenant.Name);
                Assert.NotNull(loadedParent.Tenant.Parent);
                Assert.Empty(loadedParent.Tenant.Parent);
                Assert.NotNull(loadedParent.Tenant.Successors);
                Assert.NotNull(loadedParent.Tenant.SelfProvisioningDomains);
                Assert.Contains(parentDomain, loadedParent.Tenant.SelfProvisioningDomains);

                // Load second tenant using Search
                var tenant2SearchResponse = await _descopeClient.Mgmt.V1.Tenant.Search.PostAsync(new SearchTenantsRequest
                {
                    TenantIds = new List<string> { childTenantId! }
                });
                Assert.NotNull(tenant2SearchResponse?.Tenants);
                Assert.Single(tenant2SearchResponse.Tenants);
                var loadedChild = tenant2SearchResponse.Tenants[0];
                Assert.Equal(childName, loadedChild.Name);
                Assert.Equal(parentTenantId, loadedChild.Parent);
                Assert.NotNull(loadedChild.Successors);
                Assert.Contains(grandchildTenantId, loadedChild.Successors);

                // Load third tenant
                var loadedGrandchild = await _descopeClient.Mgmt.V1.Tenant.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Id = grandchildTenantId;
                });
                Assert.NotNull(loadedGrandchild);
                Assert.NotNull(loadedGrandchild.Tenant);
                Assert.Equal(grandchildName, loadedGrandchild.Tenant.Name);
                Assert.Equal(childTenantId, loadedGrandchild.Tenant.Parent);
                Assert.NotNull(loadedGrandchild.Tenant.Successors);
                Assert.Empty(loadedGrandchild.Tenant.Successors);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(grandchildTenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = grandchildTenantId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(childTenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = childTenantId }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(parentTenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = parentTenantId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Tenant_Create_MissingName()
        {
            async Task Act() => await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(new CreateTenantRequest { Name = "" });
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The name field is required", result.Message);
        }

        [Fact]
        public async Task Tenant_UpdateAndSearch()
        {
            string? tenantId = null;
            try
            {
                // Create a tenant
                string tenantName = Guid.NewGuid().ToString();
                var createResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(new CreateTenantRequest { Name = tenantName });
                tenantId = createResponse?.Id;
                var updatedTenantName = tenantName + "updated";

                // Update and compare
                await _descopeClient.Mgmt.V1.Tenant.Update.PostAsync(new UpdateTenantRequest
                {
                    Id = tenantId,
                    Name = updatedTenantName
                });
                var searchResponse = await _descopeClient.Mgmt.V1.Tenant.Search.PostAsync(new SearchTenantsRequest
                {
                    TenantIds = new List<string> { tenantId! }
                });
                Assert.NotNull(searchResponse?.Tenants);
                Assert.Single(searchResponse.Tenants);
                Assert.Equal(updatedTenantName, searchResponse.Tenants[0].Name);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Tenant_Update_MissingId()
        {
            async Task Act() => await _descopeClient.Mgmt.V1.Tenant.Update.PostAsync(new UpdateTenantRequest { Id = "", Name = "" });
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The id field is required", result.Message);
        }

        [Fact]
        public async Task Tenant_Update_MissingName()
        {
            async Task Act() => await _descopeClient.Mgmt.V1.Tenant.Update.PostAsync(new UpdateTenantRequest { Id = "someId", Name = "" });
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The name field is required", result.Message);
        }

        [Fact]
        public async Task Tenant_DeleteAndLoadAll()
        {
            string? tenantId = null;
            try
            {
                // Create a tenant
                var createResponse = await _descopeClient.Mgmt.V1.Tenant.Create.PostAsync(new CreateTenantRequest { Name = Guid.NewGuid().ToString() });
                var id = createResponse?.Id;
                tenantId = id;

                // Delete it
                await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = id });
                tenantId = null;

                // Load all and make sure it's gone
                var loadAllResponse = await _descopeClient.Mgmt.V1.Tenant.All.GetAsync();
                Assert.NotNull(loadAllResponse?.Tenants);
                foreach (var tenant in loadAllResponse.Tenants)
                {
                    Assert.NotEqual(id, tenant.Id);
                }
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Tenant_Delete_MissingId()
        {
            async Task Act() => await _descopeClient.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = "" });
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The id field is required", result.Message);
        }
    }
}
