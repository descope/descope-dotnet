using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class Sso : ISso
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal Sso(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        public async Task<SsoTenantSettings> LoadSettings(string tenantId)
        {
            Utils.EnforceRequiredArgs(("tenantId", tenantId));
            return await _httpClient.Get<SsoTenantSettings>(Routes.SsoLoadSettings, _managementKey, queryParams: new Dictionary<string, string?> { { "tenantId", tenantId } });
        }

        public async Task ConfigureSAMLSettings(string tenantId, SsoSamlSettings settings, string? redirectUrl = null, List<string>? domains = null)
        {
            var body = new
            {
                tenantId,
                settings = new
                {
                    idpUrl = settings.IdpUrl,
                    entityId = settings.IdpEntityId,
                    idpCert = settings.IdpCertificate,
                    roleMappings = ConvertRoleMapping(settings.RoleMappings),
                    attributeMapping = settings.AttributeMapping,
                },
                redirectUrl,
                domains,
            };
            await _httpClient.Post<object>(Routes.SsoSetSaml, _managementKey, body: body);
        }

        public async Task ConfigureSamlSettingsByMetadata(string tenantId, SsoSamlSettingsByMetadata settings, string? redirectUrl = null, List<string>? domains = null)
        {
            Utils.EnforceRequiredArgs(("tenantId", tenantId), ("IdpMetadataUrl", settings.IdpMetadataUrl));
            var body = new
            {
                tenantId,
                settings = new
                {
                    idpMetadataUrl = settings.IdpMetadataUrl,
                    roleMappings = ConvertRoleMapping(settings.RoleMappings),
                    attributeMapping = settings.AttributeMapping,
                },
                redirectUrl,
                domains,
            };
            await _httpClient.Post<object>(Routes.SsoSetSamlByMetadata, _managementKey, body: body);
        }

        public async Task ConfigureOidcSettings(string tenantId, SsoOidcSettings settings, List<string>? domains = null)
        {
            Utils.EnforceRequiredArgs(("tenantId", tenantId));
            var body = new
            {
                tenantId,
                settings,
                domains,
            };
            await _httpClient.Post<object>(Routes.SsoSetOidc, _managementKey, body: body);
        }

        public async Task DeleteSettings(string tenantId)
        {
            Utils.EnforceRequiredArgs(("tenantId", tenantId));
            await _httpClient.Delete<object>(Routes.SsoDeleteSettings, _managementKey, queryParams: new Dictionary<string, string?> { { "tenantId", tenantId } });
        }

        private static List<Dictionary<string, object>> ConvertRoleMapping(List<RoleMapping>? roleMappings)
        {
            var mappings = new List<Dictionary<string, object>>();
            roleMappings?.ForEach(entry => mappings.Add(new Dictionary<string, object> { { "groups", entry.Groups }, { "roleName", entry.Role } }));
            return mappings;
        }

    }
}
