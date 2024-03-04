using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class AccessKey : IAccessKey
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal AccessKey(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        public async Task<AccessKeyCreateResponse> Create(string name, int? expireTime, List<string>? roleNames, List<AssociatedTenant>? keyTenants, string? userId)
        {
            if (string.IsNullOrEmpty(name)) throw new DescopeException("Access key name is required for creation");
            var body = new { expireTime, name, keyTenants, roleNames, userId };
            return await _httpClient.Post<AccessKeyCreateResponse>(Routes.AccessKeyCreate, _managementKey, body);
        }

        public async Task<AccessKeyResponse> Update(string id, string name)
        {
            if (string.IsNullOrEmpty(id)) throw new DescopeException("Access key ID is required for update");
            if (string.IsNullOrEmpty(name)) throw new DescopeException("Access key name cannot be updated to empty");
            var body = new { id, name };
            var result = await _httpClient.Post<LoadAccessKeyResponse>(Routes.AccessKeyUpdate, _managementKey, body);
            return result.Key;
        }

        public async Task Activate(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new DescopeException("Access key ID is required for activation");
            var request = new { id = id };
            await _httpClient.Post<object>(Routes.AccessKeyActivate, _managementKey, request);
        }

        public async Task Deactivate(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new DescopeException("Access key ID is required for deactivation");
            var request = new { id = id };
            await _httpClient.Post<object>(Routes.AccessKeyDeactivate, _managementKey, request);
        }

        public async Task Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new DescopeException("Access key ID is required for deletion");
            var request = new { id = id };
            await _httpClient.Post<object>(Routes.AccessKeyDelete, _managementKey, request);
        }

        public async Task<AccessKeyResponse> Load(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new DescopeException("Access key ID is required to load");
            var result = await _httpClient.Get<LoadAccessKeyResponse>(Routes.AccessKeyLoad + $"?id={id}", _managementKey);
            return result.Key;
        }

        public async Task<List<AccessKeyResponse>> SearchAll(List<string>? tenantIds)
        {
            var request = new { tenantIds };
            var result = await _httpClient.Post<SearchAccessKeyResponse>(Routes.AccessKeySearch, _managementKey, request);
            return result.Keys;
        }

    }

    internal class LoadAccessKeyResponse
    {
        [JsonPropertyName("key")]
        public AccessKeyResponse Key { get; set; }

        public LoadAccessKeyResponse(AccessKeyResponse key)
        {
            Key = key;
        }
    }

    public class SearchAccessKeyResponse
    {
        [JsonPropertyName("keys")]
        public List<AccessKeyResponse> Keys { get; set; }

        public SearchAccessKeyResponse(List<AccessKeyResponse> keys)
        {
            Keys = keys;
        }
    }
}
