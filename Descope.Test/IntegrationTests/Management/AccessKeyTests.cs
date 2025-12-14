using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class AccessKeyTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task AccessKey_Create_MissingName()
        {
            var createAccessKeyRequest = new CreateAccessKeyRequest
            {
                Name = ""
            };

            async Task Act() => await _descopeClient.Mgmt.V1.Accesskey.Create.PostAsync(createAccessKeyRequest);
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The name field is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_CreateAndUpdate()
        {
            string? id = null;
            try
            {
                // Create an access key
                var createAccessKeyRequest = new CreateAccessKeyRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var accessKey = await _descopeClient.Mgmt.V1.Accesskey.Create.PostAsync(createAccessKeyRequest);
                id = accessKey?.Key?.Id;

                // Update and compare
                var updatedName = accessKey?.Key?.Name + "updated";
                var updateRequest = new UpdateAccessKeyRequest
                {
                    Id = id,
                    Name = updatedName
                };
                var updatedKey = await _descopeClient.Mgmt.V1.Accesskey.Update.PostAsync(updateRequest);
                Assert.Equal(updatedName, updatedKey?.Key?.Name);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Accesskey.DeletePath.PostAsync(new AccessKeyRequest { Id = id });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task AccessKey_Update_MissingId()
        {
            var updateRequest = new UpdateAccessKeyRequest
            {
                Id = "",
                Name = "name"
            };

            async Task Act() => await _descopeClient.Mgmt.V1.Accesskey.Update.PostAsync(updateRequest);
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The id field is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_Update_MissingName()
        {
            string? id = null;
            try
            {
                // Create an access key first
                var createAccessKeyRequest = new CreateAccessKeyRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var accessKey = await _descopeClient.Mgmt.V1.Accesskey.Create.PostAsync(createAccessKeyRequest);
                id = accessKey?.Key?.Id;

                // Try to update with empty name
                var updateRequest = new UpdateAccessKeyRequest
                {
                    Id = id,
                    Name = ""
                };

                async Task Act() => await _descopeClient.Mgmt.V1.Accesskey.Update.PostAsync(updateRequest);
                DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
                // The API validates that name can't be empty
                Assert.Contains("The name field is required", result.Message);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Accesskey.DeletePath.PostAsync(new AccessKeyRequest { Id = id });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task Accesskey_ActivateDeactivate()
        {
            string? id = null;
            try
            {
                // Create an access key
                var createAccessKeyRequest = new CreateAccessKeyRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var accessKey = await _descopeClient.Mgmt.V1.Accesskey.Create.PostAsync(createAccessKeyRequest);
                id = accessKey?.Key?.Id;

                // Deactivate
                var deactivateRequest = new AccessKeyRequest { Id = id };
                await _descopeClient.Mgmt.V1.Accesskey.Deactivate.PostAsync(deactivateRequest);

                // Reload the key to verify status
                var loadedKey = await _descopeClient.Mgmt.V1.Accesskey.GetWithIdAsync(id!);
                Assert.Equal("inactive", loadedKey?.Key?.Status);

                // Activate
                var activateRequest = new AccessKeyRequest { Id = id };
                await _descopeClient.Mgmt.V1.Accesskey.Activate.PostAsync(activateRequest);

                // Reload the key to verify status
                loadedKey = await _descopeClient.Mgmt.V1.Accesskey.GetWithIdAsync(id!);
                Assert.Equal("active", loadedKey?.Key?.Status);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Accesskey.DeletePath.PostAsync(new AccessKeyRequest { Id = id });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task AccessKey_Activate_MissingId()
        {
            var activateRequest = new AccessKeyRequest { Id = "" };

            async Task Act() => await _descopeClient.Mgmt.V1.Accesskey.Activate.PostAsync(activateRequest);
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The id field is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_Deactivate_MissingId()
        {
            var deactivateRequest = new AccessKeyRequest { Id = "" };

            async Task Act() => await _descopeClient.Mgmt.V1.Accesskey.Deactivate.PostAsync(deactivateRequest);
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The id field is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_Load_MissingId()
        {
            // Test that loading with an empty ID throws an appropriate error
            async Task Act() => await _descopeClient.Mgmt.V1.Accesskey.GetWithIdAsync("");
            var result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("ID is required", result.Message);
        }

        [Fact]
        public async Task AccessKey_SearchAll()
        {
            string? id = null;
            try
            {
                // Create an access key
                var createAccessKeyRequest = new CreateAccessKeyRequest
                {
                    Name = Guid.NewGuid().ToString()
                };
                var accessKey = await _descopeClient.Mgmt.V1.Accesskey.Create.PostAsync(createAccessKeyRequest);
                id = accessKey?.Key?.Id;

                // Search for it
                var searchRequest = new SearchAccessKeysRequest();
                var accessKeys = await _descopeClient.Mgmt.V1.Accesskey.Search.PostAsync(searchRequest);
                var key = accessKeys?.Keys?.Find(key => key.Id == id);
                Assert.NotNull(key);
            }
            finally
            {
                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.Accesskey.DeletePath.PostAsync(new AccessKeyRequest { Id = id });
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task AccessKey_Delete()
        {
            // Arrange
            var createAccessKeyRequest = new CreateAccessKeyRequest
            {
                Name = Guid.NewGuid().ToString()
            };
            var accessKey = await _descopeClient.Mgmt.V1.Accesskey.Create.PostAsync(createAccessKeyRequest);

            // Act
            var deleteRequest = new AccessKeyRequest { Id = accessKey?.Key?.Id };
            await _descopeClient.Mgmt.V1.Accesskey.DeletePath.PostAsync(deleteRequest);

            // Assert - search for the key and verify it's gone
            var searchRequest = new SearchAccessKeysRequest();
            var accessKeys = await _descopeClient.Mgmt.V1.Accesskey.Search.PostAsync(searchRequest);
            var deletedKey = accessKeys?.Keys?.Find(key => key.Id == accessKey?.Key?.Id);
            Assert.Null(deletedKey);
        }

        [Fact]
        public async Task Accesskey_Delete_MissingId()
        {
            var deleteRequest = new AccessKeyRequest { Id = "" };

            async Task Act() => await _descopeClient.Mgmt.V1.Accesskey.DeletePath.PostAsync(deleteRequest);
            DescopeException result = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("The id field is required", result.Message);
        }
    }
}
