using Xunit;
using Descope.Mgmt.Models.Managementv1;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class ListTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task List_CreateTexts()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var response = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "texts",
                    Data = new UntypedArray(new List<UntypedNode>
                    {
                        new UntypedString("value1"),
                        new UntypedString("value2"),
                        new UntypedString("value3"),
                    }),
                });
                Assert.NotNull(response?.List);
                listId = response.List.Id;
                Assert.NotEmpty(listId!);
                Assert.Equal(name, response.List.Name);
                Assert.Equal("texts", response.List.Type);
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_CreateJSON()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var response = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "json",
                    Data = new UntypedObject(new Dictionary<string, UntypedNode>
                    {
                        { "key1", new UntypedString("value1") },
                        { "nested", new UntypedObject(new Dictionary<string, UntypedNode>
                            {
                                { "key2", new UntypedString("value2") },
                            })
                        },
                    }),
                });
                Assert.NotNull(response?.List);
                listId = response.List.Id;
                Assert.NotEmpty(listId!);
                Assert.Equal(name, response.List.Name);
                Assert.Equal("json", response.List.Type);
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_CreateEmpty()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var response = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "texts",
                });
                Assert.NotNull(response?.List);
                listId = response.List.Id;
                Assert.NotEmpty(listId!);
                Assert.Equal(name, response.List.Name);
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_LoadById()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var desc = "test description";
                var createResponse = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Description = desc,
                    Type = "texts",
                });
                listId = createResponse?.List?.Id;
                Assert.NotNull(listId);

                await RetryUntilSuccessAsync(async () =>
                {
                    var loadResponse = await _descopeClient.Mgmt.V1.List[listId!].GetAsync();
                    Assert.NotNull(loadResponse?.List);
                    Assert.Equal(listId, loadResponse.List.Id);
                    Assert.Equal(name, loadResponse.List.Name);
                    Assert.Equal(desc, loadResponse.List.Description);
                    Assert.Equal("texts", loadResponse.List.Type);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_LoadByName()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var createResponse = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "texts",
                });
                listId = createResponse?.List?.Id;
                Assert.NotNull(listId);

                await RetryUntilSuccessAsync(async () =>
                {
                    var loadResponse = await _descopeClient.Mgmt.V1.List.Name[name].GetAsync();
                    Assert.NotNull(loadResponse?.List);
                    Assert.Equal(listId, loadResponse.List.Id);
                    Assert.Equal(name, loadResponse.List.Name);
                    Assert.Equal("texts", loadResponse.List.Type);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_LoadAll()
        {
            string? listId1 = null;
            string? listId2 = null;
            try
            {
                var name1 = Guid.NewGuid().ToString();
                var name2 = Guid.NewGuid().ToString();

                var create1 = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest { Name = name1, Type = "texts" });
                listId1 = create1?.List?.Id;
                Assert.NotNull(listId1);

                var create2 = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest { Name = name2, Type = "texts" });
                listId2 = create2?.List?.Id;
                Assert.NotNull(listId2);

                await RetryUntilSuccessAsync(async () =>
                {
                    var allResponse = await _descopeClient.Mgmt.V1.List.All.GetAsync();
                    Assert.NotNull(allResponse?.Lists);
                    Assert.Contains(allResponse.Lists, l => l.Id == listId1);
                    Assert.Contains(allResponse.Lists, l => l.Id == listId2);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId1))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId1 }); }
                    catch { }
                }
                if (!string.IsNullOrEmpty(listId2))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId2 }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_Update()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var createResponse = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "texts",
                });
                listId = createResponse?.List?.Id;
                Assert.NotNull(listId);

                var updatedName = name + "_updated";
                var updatedDesc = "updated description";

                await _descopeClient.Mgmt.V1.List.Update.PostAsync(new UpdateListRequest
                {
                    Id = listId,
                    Name = updatedName,
                    Description = updatedDesc,
                    Type = "texts",
                    Data = new UntypedArray(new List<UntypedNode>
                    {
                        new UntypedString("value1"),
                        new UntypedString("value2"),
                        new UntypedString("value3"),
                    }),
                });

                var loadResponse = await _descopeClient.Mgmt.V1.List[listId!].GetAsync();
                Assert.NotNull(loadResponse?.List);
                Assert.Equal(updatedName, loadResponse.List.Name);
                Assert.Equal(updatedDesc, loadResponse.List.Description);
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_Delete()
        {
            var name = Guid.NewGuid().ToString();
            var createResponse = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
            {
                Name = name,
                Type = "texts",
            });
            var listId = createResponse?.List?.Id;
            Assert.NotNull(listId);

            await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId });

            await RetryUntilSuccessAsync(async () =>
            {
                await Assert.ThrowsAsync<DescopeException>(async () =>
                    await _descopeClient.Mgmt.V1.List[listId!].GetAsync());
            });
        }

        [Fact]
        public async Task List_Import()
        {
            var name1 = Guid.NewGuid().ToString();
            var name2 = Guid.NewGuid().ToString();
            try
            {
                await _descopeClient.Mgmt.V1.List.Import.PostAsync(new ImportListsRequest
                {
                    Lists = new System.Collections.Generic.List<Descope.Mgmt.Models.Managementv1.List>
                    {
                        new Descope.Mgmt.Models.Managementv1.List { Name = name1, Type = "texts" },
                        new Descope.Mgmt.Models.Managementv1.List { Name = name2, Type = "texts" },
                    },
                });

                await RetryUntilSuccessAsync(async () =>
                {
                    var r1 = await _descopeClient.Mgmt.V1.List.Name[name1].GetAsync();
                    Assert.NotNull(r1?.List?.Id);
                    var r2 = await _descopeClient.Mgmt.V1.List.Name[name2].GetAsync();
                    Assert.NotNull(r2?.List?.Id);
                });
            }
            finally
            {
                // Load by name to get IDs for cleanup
                try
                {
                    var r1 = await _descopeClient.Mgmt.V1.List.Name[name1].GetAsync();
                    if (!string.IsNullOrEmpty(r1?.List?.Id))
                    {
                        await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = r1.List.Id });
                    }
                }
                catch { }
                try
                {
                    var r2 = await _descopeClient.Mgmt.V1.List.Name[name2].GetAsync();
                    if (!string.IsNullOrEmpty(r2?.List?.Id))
                    {
                        await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = r2.List.Id });
                    }
                }
                catch { }
            }
        }

        [Fact]
        public async Task List_CreateIPs()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var response = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "ips",
                    Data = new UntypedArray(new List<UntypedNode>
                    {
                        new UntypedString("1.2.3.4"),
                        new UntypedString("5.6.7.8"),
                    }),
                });
                Assert.NotNull(response?.List);
                listId = response.List.Id;
                Assert.NotEmpty(listId!);
                Assert.Equal(name, response.List.Name);
                Assert.Equal("ips", response.List.Type);
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_CheckIP()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var createResponse = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "ips",
                    Data = new UntypedArray(new List<UntypedNode>
                    {
                        new UntypedString("1.2.3.4"),
                        new UntypedString("5.6.7.8"),
                    }),
                });
                listId = createResponse?.List?.Id;
                Assert.NotNull(listId);

                await RetryUntilSuccessAsync(async () =>
                {
                    // Check existing IP
                    var existsResponse = await _descopeClient.Mgmt.V1.List.Ip.Check.PostAsync(new CheckIPInListRequest
                    {
                        Id = listId,
                        Ip = "1.2.3.4",
                    });
                    Assert.True(existsResponse?.Exists);

                    // Check non-existing IP
                    var notExistsResponse = await _descopeClient.Mgmt.V1.List.Ip.Check.PostAsync(new CheckIPInListRequest
                    {
                        Id = listId,
                        Ip = "9.9.9.9",
                    });
                    Assert.False(notExistsResponse?.Exists);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_Error_LoadNotFound()
        {
            async Task Act() => await _descopeClient.Mgmt.V1.List["nonexistent-id-that-does-not-exist"].GetAsync();
            await Assert.ThrowsAsync<DescopeException>(Act);
        }

        [Fact]
        public async Task List_Error_LoadByNameNotFound()
        {
            async Task Act() => await _descopeClient.Mgmt.V1.List.Name["nonexistent-name-that-does-not-exist"].GetAsync();
            await Assert.ThrowsAsync<DescopeException>(Act);
        }

        [Fact]
        public async Task List_Error_DeleteNotFound()
        {
            async Task Act() => await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = "nonexistent-id-that-does-not-exist" });
            await Assert.ThrowsAsync<DescopeException>(Act);
        }

        [Fact]
        public async Task List_AddIPs()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var createResponse = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "ips",
                    Data = new UntypedArray(new List<UntypedNode>
                    {
                        new UntypedString("1.2.3.4"),
                    }),
                });
                listId = createResponse?.List?.Id;
                Assert.NotNull(listId);

                await _descopeClient.Mgmt.V1.List.Ip.Add.PostAsync(new AddIPsToListRequest
                {
                    Id = listId,
                    Ips = new System.Collections.Generic.List<string> { "5.6.7.8", "9.10.11.12" },
                });

                await RetryUntilSuccessAsync(async () =>
                {
                    var checkResponse = await _descopeClient.Mgmt.V1.List.Ip.Check.PostAsync(new CheckIPInListRequest
                    {
                        Id = listId,
                        Ip = "5.6.7.8",
                    });
                    Assert.True(checkResponse?.Exists);
                    checkResponse = await _descopeClient.Mgmt.V1.List.Ip.Check.PostAsync(new CheckIPInListRequest
                    {
                        Id = listId,
                        Ip = "1.2.3.4",
                    });
                    Assert.True(checkResponse?.Exists);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_RemoveIPs()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var createResponse = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "ips",
                    Data = new UntypedArray(new List<UntypedNode>
                    {
                        new UntypedString("1.2.3.4"),
                        new UntypedString("5.6.7.8"),
                    }),
                });
                listId = createResponse?.List?.Id;
                Assert.NotNull(listId);

                await _descopeClient.Mgmt.V1.List.Ip.Remove.PostAsync(new RemoveIPsFromListRequest
                {
                    Id = listId,
                    Ips = new System.Collections.Generic.List<string> { "1.2.3.4" },
                });

                await RetryUntilSuccessAsync(async () =>
                {
                    var removedCheck = await _descopeClient.Mgmt.V1.List.Ip.Check.PostAsync(new CheckIPInListRequest
                    {
                        Id = listId,
                        Ip = "1.2.3.4",
                    });
                    Assert.False(removedCheck?.Exists);

                    var remainingCheck = await _descopeClient.Mgmt.V1.List.Ip.Check.PostAsync(new CheckIPInListRequest
                    {
                        Id = listId,
                        Ip = "5.6.7.8",
                    });
                    Assert.True(remainingCheck?.Exists);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task List_Clear()
        {
            string? listId = null;
            try
            {
                var name = Guid.NewGuid().ToString();
                var createResponse = await _descopeClient.Mgmt.V1.List.PostAsync(new CreateListRequest
                {
                    Name = name,
                    Type = "ips",
                    Data = new UntypedArray(new List<UntypedNode>
                    {
                        new UntypedString("1.2.3.4"),
                        new UntypedString("5.6.7.8"),
                    }),
                });
                listId = createResponse?.List?.Id;
                Assert.NotNull(listId);

                await _descopeClient.Mgmt.V1.List.Clear.PostAsync(new ClearListRequest { Id = listId });

                await RetryUntilSuccessAsync(async () =>
                {
                    var checkResponse = await _descopeClient.Mgmt.V1.List.Ip.Check.PostAsync(new CheckIPInListRequest
                    {
                        Id = listId,
                        Ip = "1.2.3.4",
                    });
                    Assert.False(checkResponse?.Exists);
                });
            }
            finally
            {
                if (!string.IsNullOrEmpty(listId))
                {
                    try { await _descopeClient.Mgmt.V1.List.DeletePath.PostAsync(new DeleteListRequest { Id = listId }); }
                    catch { }
                }
            }
        }
    }
}
