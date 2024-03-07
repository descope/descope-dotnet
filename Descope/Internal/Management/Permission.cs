using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class Permission : IPermission
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal Permission(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        public async Task Create(string name, string? description = null)
        {
            if (string.IsNullOrEmpty(name)) throw new DescopeException("name is required for creation");
            var body = new { name, description };
            await _httpClient.Post<object>(Routes.PermissionCreate, _managementKey, body);
        }

        public async Task Update(string name, string newName, string? description = null)
        {
            if (string.IsNullOrEmpty(name)) throw new DescopeException("name is required for update");
            if (string.IsNullOrEmpty(newName)) throw new DescopeException("new name cannot be updated to empty");
            var body = new { name, newName, description };
            await _httpClient.Post<object>(Routes.PermissionUpdate, _managementKey, body);
        }

        public async Task Delete(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new DescopeException("name is required for deletion");
            var body = new { name };
            await _httpClient.Post<object>(Routes.PermissionDelete, _managementKey, body);
        }

        public async Task<List<PermissionResponse>> LoadAll()
        {
            var permissionList = await _httpClient.Get<PermissionListResponse>(Routes.PermissionLoadAll, _managementKey);
            return permissionList.Permissions;
        }
    }

    internal class PermissionListResponse
    {
        [JsonPropertyName("permissions")]
        public List<PermissionResponse> Permissions { get; set; }

        public PermissionListResponse(List<PermissionResponse> permissions)
        {
            Permissions = permissions;
        }
    }
}
