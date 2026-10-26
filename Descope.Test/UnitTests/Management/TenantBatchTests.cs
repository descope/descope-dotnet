using System.Net;
using System.Text;
using System.Text.Json;
using Descope;
using Descope.Mgmt.Models.Managementv1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Xunit;

namespace Descope.Test.UnitTests.Management;

/// <summary>
/// Unit tests for the tenant patch and batch extension methods.
/// Requests go through a real HTTP request adapter so the method, path, Authorization header
/// and JSON body are checked as they are sent on the wire.
/// </summary>
public class TenantBatchTests
{
    private const string TestProjectId = "test_project_id";
    private const string TestManagementKey = "test-management-key";
    private const string BaseUrl = "https://api.descope.com";

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public CapturingHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }
        public string? Authorization { get; private set; }
        public JsonElement Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Path = request.RequestUri!.AbsolutePath;
            Authorization = request.Headers.Authorization?.ToString();
            var body = await request.Content!.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            Body = document.RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    private static IDescopeClient CreateClient(CapturingHandler handler)
    {
        var options = new DescopeClientOptions { ProjectId = TestProjectId, ManagementKey = TestManagementKey, BaseUrl = BaseUrl };
        var httpClient = new HttpClient(handler);
        var mgmtAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(TestProjectId, TestManagementKey), httpClient: httpClient) { BaseUrl = BaseUrl };
        var authAdapter = new HttpClientRequestAdapter(new DescopeAuthenticationProvider(TestProjectId), httpClient: httpClient) { BaseUrl = BaseUrl };
        return DescopeManagementClientFactory.CreateForTest(authAdapter, mgmtAdapter, options, httpClient);
    }

    private static List<string> Strings(JsonElement element)
    {
        return element.EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    // Patch

    [Fact]
    public async Task Tenant_PatchWithIdAsync_SendsRequest()
    {
        // Arrange
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);
        var request = new PatchTenantRequest
        {
            Name = "New Name",
            Disabled = false,
            EnforceSSO = true,
            RoleInheritance = "none",
            SelfProvisioningDomains = new UntypedArray(new List<UntypedNode> { new UntypedString("example.com") }),
            EnforceSSOExclusions = new UntypedArray(new List<UntypedNode>()),
            FederatedAppIds = new UntypedArray(new List<UntypedNode> { new UntypedString("app1") }),
            CustomAttributes = new PatchTenantRequest_customAttributes
            {
                AdditionalData = new Dictionary<string, object> { { "attr", "value" } }
            },
        };

        // Act
        await client.Mgmt.V1.Tenant.PatchPath.PatchWithIdAsync("tenant-id", request);

        // Assert
        handler.Method.Should().Be(HttpMethod.Patch);
        handler.Path.Should().Be("/v1/mgmt/tenant/patch");
        handler.Authorization.Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        var body = handler.Body;
        body.GetProperty("id").GetString().Should().Be("tenant-id");
        body.GetProperty("name").GetString().Should().Be("New Name");
        body.GetProperty("disabled").GetBoolean().Should().BeFalse();
        body.GetProperty("enforceSSO").GetBoolean().Should().BeTrue();
        body.GetProperty("roleInheritance").GetString().Should().Be("none");
        Strings(body.GetProperty("selfProvisioningDomains")).Should().Equal("example.com");
        body.GetProperty("enforceSSOExclusions").GetArrayLength().Should().Be(0);
        Strings(body.GetProperty("federatedAppIds")).Should().Equal("app1");
        body.GetProperty("customAttributes").GetProperty("attr").GetString().Should().Be("value");
        body.TryGetProperty("authType", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Tenant_PatchWithIdAsync_OmitsUnsetFields()
    {
        // Arrange
        var handler = new CapturingHandler("{}");
        var client = CreateClient(handler);

        // Act
        await client.Mgmt.V1.Tenant.PatchPath.PatchWithIdAsync("tenant-id", new PatchTenantRequest { Disabled = true });

        // Assert
        handler.Body.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "disabled");
    }

    [Fact]
    public async Task Tenant_PatchWithIdAsync_ThrowsDescopeException_WhenIdIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.PatchPath.PatchWithIdAsync("", new PatchTenantRequest { Name = "name" });
        });
    }

    [Fact]
    public async Task Tenant_PatchWithIdAsync_ThrowsDescopeException_WhenRequestIsNull()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.PatchPath.PatchWithIdAsync("tenant-id", null!);
        });
    }

    [Fact]
    public async Task Tenant_PatchWithIdAsync_ThrowsDescopeException_WhenNameIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.PatchPath.PatchWithIdAsync("tenant-id", new PatchTenantRequest { Name = "" });
        });
    }

    [Fact]
    public async Task Tenant_PatchWithIdAsync_ThrowsDescopeException_WhenListItemIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        var emptyItem = new UntypedArray(new List<UntypedNode> { new UntypedString("") });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.PatchPath.PatchWithIdAsync("tenant-id", new PatchTenantRequest { SelfProvisioningDomains = emptyItem });
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.PatchPath.PatchWithIdAsync("tenant-id", new PatchTenantRequest { EnforceSSOExclusions = emptyItem });
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.PatchPath.PatchWithIdAsync("tenant-id", new PatchTenantRequest { FederatedAppIds = emptyItem });
        });
    }

    // Create batch

    [Fact]
    public async Task Tenant_CreateBatch_PostWithTenantsAsync_SendsRequestAndParsesResponse()
    {
        // Arrange
        var handler = new CapturingHandler(@"{
            ""createdTenants"": [{""id"": ""tenant-1"", ""name"": ""Tenant 1""}],
            ""failedTenants"": [{""failure"": ""already exists"", ""tenant"": {""name"": ""Tenant 2"", ""id"": ""tenant-2"", ""enforceSSO"": true}}],
            ""additionalErrors"": {""tenant-3"": ""some error""},
            ""unknownField"": 1
        }");
        var client = CreateClient(handler);
        var tenants = new List<CreateTenantRequest>
        {
            new CreateTenantRequest
            {
                Name = "Tenant 1",
                Id = "tenant-1",
                SelfProvisioningDomains = new List<string> { "example.com" },
                AuthType = "saml",
                Disabled = true,
                EnforceSSO = true,
                EnforceSSOExclusions = new List<string> { "user-1" },
                FederatedAppIds = new List<string> { "app1" },
                Parent = "parent-tenant",
                RoleInheritance = "none",
            },
        };

        // Act
        var response = await client.Mgmt.V1.Tenant.Create.Batch.PostWithTenantsAsync(tenants);

        // Assert
        handler.Method.Should().Be(HttpMethod.Post);
        handler.Path.Should().Be("/v1/mgmt/tenant/create/batch");
        handler.Authorization.Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        var sent = handler.Body.GetProperty("tenants");
        sent.GetArrayLength().Should().Be(1);
        var tenant = sent[0];
        tenant.GetProperty("name").GetString().Should().Be("Tenant 1");
        tenant.GetProperty("id").GetString().Should().Be("tenant-1");
        Strings(tenant.GetProperty("selfProvisioningDomains")).Should().Equal("example.com");
        tenant.GetProperty("authType").GetString().Should().Be("saml");
        tenant.GetProperty("disabled").GetBoolean().Should().BeTrue();
        tenant.GetProperty("enforceSSO").GetBoolean().Should().BeTrue();
        Strings(tenant.GetProperty("enforceSSOExclusions")).Should().Equal("user-1");
        Strings(tenant.GetProperty("federatedAppIds")).Should().Equal("app1");
        tenant.GetProperty("parent").GetString().Should().Be("parent-tenant");
        tenant.GetProperty("roleInheritance").GetString().Should().Be("none");

        response.Should().NotBeNull();
        response!.CreatedTenants.Should().ContainSingle();
        response.CreatedTenants![0].Id.Should().Be("tenant-1");
        response.CreatedTenants[0].Name.Should().Be("Tenant 1");
        response.FailedTenants.Should().ContainSingle();
        response.FailedTenants![0].Failure.Should().Be("already exists");
        response.FailedTenants[0].Tenant!.Id.Should().Be("tenant-2");
        response.FailedTenants[0].Tenant!.Name.Should().Be("Tenant 2");
        response.FailedTenants[0].Tenant!.EnforceSSO.Should().BeTrue();
        response.AdditionalErrors!.AdditionalData.Should().ContainKey("tenant-3");
        response.AdditionalErrors.AdditionalData["tenant-3"].ToString().Should().Be("some error");
    }

    [Fact]
    public async Task Tenant_CreateBatch_PostWithTenantsAsync_ThrowsDescopeException_WhenTenantsIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Create.Batch.PostWithTenantsAsync(null!);
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Create.Batch.PostWithTenantsAsync(new List<CreateTenantRequest>());
        });
    }

    [Fact]
    public async Task Tenant_CreateBatch_PostWithTenantsAsync_ThrowsDescopeException_WhenNameIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Create.Batch.PostWithTenantsAsync(new List<CreateTenantRequest> { new CreateTenantRequest { Id = "tenant-1" } });
        });
    }

    [Fact]
    public async Task Tenant_CreateBatch_PostWithTenantsAsync_ThrowsDescopeException_WhenListItemIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        var empty = new List<string> { "" };
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Create.Batch.PostWithTenantsAsync(new List<CreateTenantRequest> { new CreateTenantRequest { Name = "n", SelfProvisioningDomains = empty } });
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Create.Batch.PostWithTenantsAsync(new List<CreateTenantRequest> { new CreateTenantRequest { Name = "n", EnforceSSOExclusions = empty } });
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Create.Batch.PostWithTenantsAsync(new List<CreateTenantRequest> { new CreateTenantRequest { Name = "n", FederatedAppIds = empty } });
        });
    }

    // Update batch

    [Fact]
    public async Task Tenant_UpdateBatch_PostWithTenantsAsync_SendsRequestAndParsesResponse()
    {
        // Arrange
        var handler = new CapturingHandler(@"{
            ""updatedTenantIds"": [""tenant-1""],
            ""failedTenants"": [{""failure"": ""not found"", ""tenant"": {""id"": ""tenant-2"", ""name"": ""Tenant 2""}}],
            ""additionalErrors"": {}
        }");
        var client = CreateClient(handler);
        var tenants = new List<UpdateTenantRequest>
        {
            new UpdateTenantRequest
            {
                Id = "tenant-1",
                Name = "Tenant 1",
                SelfProvisioningDomains = new List<string> { "example.com" },
                AuthType = "oidc",
                Disabled = false,
                EnforceSSO = true,
                EnforceSSOExclusions = new List<string> { "user-1" },
                FederatedAppIds = new List<string> { "app1" },
                RoleInheritance = "none",
            },
        };

        // Act
        var response = await client.Mgmt.V1.Tenant.Update.Batch.PostWithTenantsAsync(tenants);

        // Assert
        handler.Method.Should().Be(HttpMethod.Post);
        handler.Path.Should().Be("/v1/mgmt/tenant/update/batch");
        handler.Authorization.Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        var tenant = handler.Body.GetProperty("tenants")[0];
        tenant.GetProperty("id").GetString().Should().Be("tenant-1");
        tenant.GetProperty("name").GetString().Should().Be("Tenant 1");
        Strings(tenant.GetProperty("selfProvisioningDomains")).Should().Equal("example.com");
        tenant.GetProperty("authType").GetString().Should().Be("oidc");
        tenant.GetProperty("disabled").GetBoolean().Should().BeFalse();
        tenant.GetProperty("enforceSSO").GetBoolean().Should().BeTrue();
        Strings(tenant.GetProperty("enforceSSOExclusions")).Should().Equal("user-1");
        Strings(tenant.GetProperty("federatedAppIds")).Should().Equal("app1");
        tenant.GetProperty("roleInheritance").GetString().Should().Be("none");

        response.Should().NotBeNull();
        response!.UpdatedTenantIds.Should().Equal("tenant-1");
        response.FailedTenants.Should().ContainSingle();
        response.FailedTenants![0].Failure.Should().Be("not found");
        response.FailedTenants[0].Tenant!.Id.Should().Be("tenant-2");
        response.FailedTenants[0].Tenant!.Name.Should().Be("Tenant 2");
        response.AdditionalErrors!.AdditionalData.Should().BeEmpty();
    }

    [Fact]
    public async Task Tenant_UpdateBatch_PostWithTenantsAsync_ThrowsDescopeException_WhenTenantsIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Update.Batch.PostWithTenantsAsync(null!);
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Update.Batch.PostWithTenantsAsync(new List<UpdateTenantRequest>());
        });
    }

    [Fact]
    public async Task Tenant_UpdateBatch_PostWithTenantsAsync_ThrowsDescopeException_WhenIdIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Update.Batch.PostWithTenantsAsync(new List<UpdateTenantRequest> { new UpdateTenantRequest { Name = "n" } });
        });
    }

    [Fact]
    public async Task Tenant_UpdateBatch_PostWithTenantsAsync_ThrowsDescopeException_WhenNameIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Update.Batch.PostWithTenantsAsync(new List<UpdateTenantRequest> { new UpdateTenantRequest { Id = "tenant-1" } });
        });
    }

    [Fact]
    public async Task Tenant_UpdateBatch_PostWithTenantsAsync_ThrowsDescopeException_WhenListItemIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        var empty = new List<string> { "" };
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Update.Batch.PostWithTenantsAsync(new List<UpdateTenantRequest> { new UpdateTenantRequest { Id = "t", Name = "n", SelfProvisioningDomains = empty } });
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Update.Batch.PostWithTenantsAsync(new List<UpdateTenantRequest> { new UpdateTenantRequest { Id = "t", Name = "n", EnforceSSOExclusions = empty } });
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.Update.Batch.PostWithTenantsAsync(new List<UpdateTenantRequest> { new UpdateTenantRequest { Id = "t", Name = "n", FederatedAppIds = empty } });
        });
    }

    // Delete batch

    [Fact]
    public async Task Tenant_DeleteBatch_PostWithIdsAsync_SendsRequestAndParsesResponse()
    {
        // Arrange
        var handler = new CapturingHandler(@"{
            ""deletedTenantIds"": [""tenant-1""],
            ""failedTenants"": [{""failure"": ""not found"", ""id"": ""tenant-2""}],
            ""additionalErrors"": {""general"": ""partial failure""}
        }");
        var client = CreateClient(handler);

        // Act
        var response = await client.Mgmt.V1.Tenant.DeletePath.Batch.PostWithIdsAsync(new List<string> { "tenant-1", "tenant-2" }, cascade: true);

        // Assert
        handler.Method.Should().Be(HttpMethod.Post);
        handler.Path.Should().Be("/v1/mgmt/tenant/delete/batch");
        handler.Authorization.Should().Be($"Bearer {TestProjectId}:{TestManagementKey}");
        Strings(handler.Body.GetProperty("ids")).Should().Equal("tenant-1", "tenant-2");
        handler.Body.GetProperty("cascade").GetBoolean().Should().BeTrue();

        response.Should().NotBeNull();
        response!.DeletedTenantIds.Should().Equal("tenant-1");
        response.FailedTenants.Should().ContainSingle();
        response.FailedTenants![0].Failure.Should().Be("not found");
        response.FailedTenants[0].Id.Should().Be("tenant-2");
        response.AdditionalErrors!.AdditionalData["general"].ToString().Should().Be("partial failure");
    }

    [Fact]
    public async Task Tenant_DeleteBatch_PostWithIdsAsync_ThrowsDescopeException_WhenIdsIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.DeletePath.Batch.PostWithIdsAsync(null!);
        });
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.DeletePath.Batch.PostWithIdsAsync(new List<string>());
        });
    }

    [Fact]
    public async Task Tenant_DeleteBatch_PostWithIdsAsync_ThrowsDescopeException_WhenIdIsEmpty()
    {
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Mgmt.V1.Tenant.DeletePath.Batch.PostWithIdsAsync(new List<string> { "tenant-1", "" });
        });
    }
}
