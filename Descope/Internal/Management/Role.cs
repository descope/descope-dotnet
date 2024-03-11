using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class Role : IRole
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal Role(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        public async Task Create(string name, string? description = null, List<string>? permissionNames = null, string? tenantId = null)
        {
            if (string.IsNullOrEmpty(name)) throw new DescopeException("name is required for creation");
            var body = new { name, description, permissionNames, tenantId };
            await _httpClient.Post<object>(Routes.RoleCreate, _managementKey, body);
        }

        public async Task Update(string name, string newName, string? description = null, List<string>? permissionNames = null, string? tenantId = null)
        {
            if (string.IsNullOrEmpty(name)) throw new DescopeException("name is required for update");
            if (string.IsNullOrEmpty(newName)) throw new DescopeException("new name cannot be updated to empty");
            var body = new { name, newName, description, permissionNames, tenantId };
            await _httpClient.Post<object>(Routes.RoleUpdate, _managementKey, body);
        }

        public async Task Delete(string name, string? tenantId)
        {
            if (string.IsNullOrEmpty(name)) throw new DescopeException("name is required for deletion");
            var body = new { name, tenantId };
            await _httpClient.Post<object>(Routes.RoleDelete, _managementKey, body);
        }

        public async Task<List<RoleResponse>> LoadAll()
        {
            var roleList = await _httpClient.Get<RoleListResponse>(Routes.RoleLoadAll, _managementKey);
            return roleList.Roles;
        }

        public async Task<List<RoleResponse>> SearchAll(RoleSearchOptions? options)
        {
            var roleList = await _httpClient.Post<RoleListResponse>(Routes.RoleSearchAll, _managementKey, options);
            return roleList.Roles;
        }
    }

    internal class RoleListResponse
    {
        [JsonPropertyName("roles")]
        public List<RoleResponse> Roles { get; set; }

        public RoleListResponse(List<RoleResponse> roles)
        {
            Roles = roles;
        }
    }
}
