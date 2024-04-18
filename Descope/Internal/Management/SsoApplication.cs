
using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class SsoApplication : ISsoApplication
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal SsoApplication(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        public async Task<string> CreateOidcApplication(OidcApplicationOptions options)
        {
            var resp = await _httpClient.Post<SsoApplicationCreateResponse>(Routes.SsoApplicationOidcCreate, _managementKey, body: options);
            return resp.Id;
        }

        public async Task<string> CreateSAMLApplication(SamlApplicationOptions options)
        {
            var resp = await _httpClient.Post<SsoApplicationCreateResponse>(Routes.SsoApplicationSamlCreate, _managementKey, body: options);
            return resp.Id;
        }

        public async Task UpdateOidcApplication(OidcApplicationOptions options)
        {
            await _httpClient.Post<object>(Routes.SsoApplicationOidcUpdate, _managementKey, body: options);
        }

        public async Task UpdateSamlApplication(SamlApplicationOptions options)
        {
            await _httpClient.Post<object>(Routes.SsoApplicationSamlUpdate, _managementKey, body: options);
        }

        public async Task Delete(string id)
        {
            var body = new { id };
            await _httpClient.Post<object>(Routes.SsoApplicationDelete, _managementKey, body: body);
        }

        public async Task<SsoApplicationResponse> Load(string id)
        {
            return await _httpClient.Get<SsoApplicationResponse>(Routes.SsoApplicationLoad, _managementKey, queryParams: new Dictionary<string, string?> { { "id", id } });
        }

        public async Task<List<SsoApplicationResponse>> LoadAll()
        {
            var resp = await _httpClient.Get<SsoLoadAllResponse>(Routes.SsoApplicationLoadAll, _managementKey);
            return resp.Apps;
        }

    }

    internal class SsoApplicationCreateResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        public SsoApplicationCreateResponse(string id)
        {
            Id = id;
        }
    }

    internal class SsoLoadAllResponse
    {
        [JsonPropertyName("apps")]
        public List<SsoApplicationResponse> Apps { get; set; }

        public SsoLoadAllResponse(List<SsoApplicationResponse> apps)
        {
            Apps = apps;
        }
    }

}
