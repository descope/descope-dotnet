using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class Tenant : ITenant
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal Tenant(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        public async Task<string> Create(TenantOptions options, string? id = null)
        {
            if (string.IsNullOrEmpty(options.Name)) throw new DescopeException("Tenant name is required for creation");
            var body = new
            {
                id,
                name = options.Name,
                selfProvisioningDomains = options.SelfProvisioningDomains,
                customAttributes = options.CustomAttributes,
                parent = options.Parent
            };
            var result = await _httpClient.Post<TenantServerResponse>(Routes.TenantCreate, _managementKey, body);
            return result.Id;
        }

        public async Task Update(string id, TenantOptions options)
        {
            if (string.IsNullOrEmpty(id)) throw new DescopeException("Tenant ID is required for update");
            if (string.IsNullOrEmpty(options.Name)) throw new DescopeException("Tenant name cannot be updated to empty");
            var body = new
            {
                id,
                name = options.Name,
                selfProvisioningDomains = options.SelfProvisioningDomains,
                customAttributes = options.CustomAttributes
            };
            await _httpClient.Post<TenantServerResponse>(Routes.TenantUpdate, _managementKey, body);
        }

        public async Task Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new DescopeException("Tenant ID is required for deletion");
            var body = new { id };
            await _httpClient.Post<object>(Routes.TenantDelete, _managementKey, body);
        }

        public async Task<TenantResponse> LoadById(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new DescopeException("Tenant ID is required to load By ID");
            return await _httpClient.Get<TenantResponse>(Routes.TenantLoad + $"?id={id}", _managementKey);
        }

        public async Task<List<TenantResponse>> LoadAll()
        {
            var tenantList = await _httpClient.Get<TenantListResponse>(Routes.TenantLoadAll, _managementKey);
            return tenantList.Tenants;
        }

        public async Task<List<TenantResponse>> SearchAll(TenantSearchOptions? options)
        {
            var body = new
            {
                tenantIds = options?.Ids,
                tenantNames = options?.Names,
                tenantSelfProvisioningDomains = options?.SelfProvisioningDomains,
                customAttributes = options?.CustomAttributes,
                authType = options?.AuthType,
            };
            var tenantList = await _httpClient.Post<TenantListResponse>(Routes.TenantSearch, _managementKey, body);
            return tenantList.Tenants;
        }

    }

    internal class TenantServerResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonConstructor]
        public TenantServerResponse(string id)
        {
            Id = id;
        }
    }

    internal class TenantListResponse
    {
        [JsonPropertyName("tenants")]
        public List<TenantResponse> Tenants { get; set; }

        public TenantListResponse(List<TenantResponse> tenants)
        {
            Tenants = tenants;
        }
    }
}
