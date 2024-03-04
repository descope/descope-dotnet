using Xunit;

namespace Descope.Test.Integration
{
    public class AccessKeyTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task AccessKey_Create_MissingName()
        {
            async Task Act() => await _descopeClient.Management.AccessKey.Create(name: "");
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("Access key name is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_CreateAndUpdate()
        {
            string? id = null;
            try
            {
                // Create an access key
                var accessKey = await _descopeClient.Management.AccessKey.Create(name: Guid.NewGuid().ToString());
                id = accessKey.Key.Id;

                // Update and compare
                var updatedName = accessKey.Key.Name + "updated";
                var updatedKey = await _descopeClient.Management.AccessKey.Update(id: id, name: updatedName);
                Assert.Equal(updatedKey.Name, updatedKey.Name);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try { await _descopeClient.Management.AccessKey.Delete(id); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task AccessKey_Update_MissingId()
        {
            async Task Act() => await _descopeClient.Management.AccessKey.Update("", "name");
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("ID is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_Update_MissingName()
        {
            async Task Act() => await _descopeClient.Management.AccessKey.Update("someId", "");
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("name cannot be updated to empty", result.Message);
        }

        [Fact]
        public async Task Accesskey_ActivateDeactivate()
        {
            string? id = null;
            try
            {
                // Create an access key
                var accessKey = await _descopeClient.Management.AccessKey.Create(name: Guid.NewGuid().ToString());
                id = accessKey.Key.Id;

                // Deactivate
                await _descopeClient.Management.AccessKey.Deactivate(id);
                var loadedKey = await _descopeClient.Management.AccessKey.Load(id);
                Assert.Equal("inactive", loadedKey.Status);

                // Activate
                await _descopeClient.Management.AccessKey.Activate(id);
                loadedKey = await _descopeClient.Management.AccessKey.Load(id);
                Assert.Equal("active", loadedKey.Status);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try { await _descopeClient.Management.AccessKey.Delete(id); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task AccessKey_Activate_MissingId()
        {
            async Task Act() => await _descopeClient.Management.AccessKey.Activate("");
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("ID is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_Deactivate_MissingId()
        {
            async Task Act() => await _descopeClient.Management.AccessKey.Deactivate("");
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("ID is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_Load_MissingId()
        {
            async Task Act() => await _descopeClient.Management.AccessKey.Load("");
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("Access key ID is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_SearchAll()
        {
            string? id = null;
            try
            {
                // Create an access key
                var accessKey = await _descopeClient.Management.AccessKey.Create(name: Guid.NewGuid().ToString());
                id = accessKey.Key.Id;

                // Search for it
                var accessKeys = await _descopeClient.Management.AccessKey.SearchAll();
                var key = accessKeys.Find(key => key.Id == id);
                Assert.NotNull(key);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try { await _descopeClient.Management.AccessKey.Delete(id); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task AccessKey_Delete()
        {
            // Arrange
            var accessKey = await _descopeClient.Management.AccessKey.Create(name: Guid.NewGuid().ToString());

            // Act
            await _descopeClient.Management.AccessKey.Delete(accessKey.Key.Id);

            // Assert
            var accessKeys = await _descopeClient.Management.AccessKey.SearchAll(new List<string>() { accessKey.Key.Id });
            Assert.Empty(accessKeys);
        }

        [Fact]
        public async Task Accesskey_Delete_MissingId()
        {
            async Task Act() => await _descopeClient.Management.AccessKey.Delete("");
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("Access key ID is required", result.Message);
        }

    }
}
