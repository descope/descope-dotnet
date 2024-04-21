using System.Text.Json.Serialization;

namespace Descope
{

    /// <summary>
    /// Options to create or update an OIDC application.
    /// </summary>
    public class OidcApplicationOptions
    {
        /// <summary>
        /// Optional SSO application ID. If not provided, will be auto-generated.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        /// <summary>
        /// The SSO application's name. Must be unique per project.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Optional SSO application description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        /// <summary>
        /// Optionally set the SSO application as enabled or disabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        /// <summary>
        /// Optional SSO application logo.
        /// </summary>
        [JsonPropertyName("logo")]
        public string? Logo { get; set; }
        /// <summary>
        /// The URL where login page is hosted.
        /// </summary>
        [JsonPropertyName("loginPageUrl")]
        public string LoginPageUrl { get; set; }
        public OidcApplicationOptions(string name, string loginPageUrl)
        {
            Name = name;
            LoginPageUrl = loginPageUrl;
        }
    }

    /// <summary>
    /// Options to create or update a SAML application.
    /// </summary>
    public class SamlApplicationOptions
    {
        /// <summary>
        /// Optional SSO application ID. If not provided, will be auto-generated.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        /// <summary>
        /// The SSO application's name. Must be unique per project.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Optional SSO application description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        /// <summary>
        /// Optionally set the SSO application as enabled or disabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        /// <summary>
        /// Optional SSO application logo.
        /// </summary>
        [JsonPropertyName("logo")]
        public string? Logo { get; set; }
        /// <summary>
        /// The URL where login page is hosted.
        /// </summary>
        [JsonPropertyName("loginPageUrl")]
        public string LoginPageUrl { get; set; }
        /// <summary>
        /// Optionally determine whether SP info should be automatically fetched from <c>MetadataURL</c>
        /// or by specifying it explicitly via the EntityId, AcsUrl and Certificate properties.
        /// </summary>
        [JsonPropertyName("useMetadataInfo")]
        public bool UseMetadataInfo { get; set; }
        /// <summary>
        /// Optional SP metadata URL to fetch the SP SAML info from. Required if <c>UseMetadataInfo</c> is <c>true</c>.
        /// </summary>
        [JsonPropertyName("metadataUrl")]
        public string? MetadataURL { get; set; }
        /// <summary>
        /// Optional SP entity ID. Required if <c>UseMetadataInfo</c> is <c>false</c>.
        /// </summary>
        [JsonPropertyName("entityId")]
        public string? EntityId { get; set; }
        /// <summary>
        /// Optional SP ACS URL (SAML callback). Required if <c>UseMetadataInfo</c> is <c>false</c>.
        /// </summary>
        [JsonPropertyName("acsUrl")]
        public string? AcsUrl { get; set; }
        /// <summary>
        /// Optional SP certificate. Required only when SAML request must be signed and <c>UseMetadataInfo</c> is <c>false</c>.
        /// </summary>
        [JsonPropertyName("certificate")]
        public string? Certificate { get; set; }
        /// <summary>
        /// Optional mappings of Descope (IdP) attributes to SP attributes.
        /// </summary>
        [JsonPropertyName("attributeMapping")]
        public List<SamlIdpAttributeMappingInfo>? AttributeMapping { get; set; }
        /// <summary>
        /// Optional mappings of Descope (IdP) roles to SP groups.
        /// </summary>
        [JsonPropertyName("groupsMapping")]
        public List<SamlIdpGroupsMappingInfo>? GroupsMapping { get; set; }
        /// <summary>
        /// Optional list of URL wildcards. If provided, only URLs from this list will be accepted when receiving SAML callback requests.
        /// </summary>
        [JsonPropertyName("acsAllowedCallbacks")]
        public List<string>? AcsAllowedCallbacks { get; set; }
        /// <summary>
        /// Optionally define the SAML Assertion for the subject name type. Leave empty to use the Descope user ID or set to "email"/"phone".
        /// </summary>
        [JsonPropertyName("subjectNameIdType")]
        public string? SubjectNameIDType { get; set; }
        /// <summary>
        /// Optionally define the SAML Assertion for subject name format. Defaults to "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified".
        /// </summary>
        [JsonPropertyName("subjectNameIdFormat")]
        public string? SubjectNameIDFormat { get; set; }
        public SamlApplicationOptions(string name, string loginPageURL)
        {
            Name = name;
            LoginPageUrl = loginPageURL;
        }
    }


    public class SamlIdpAttributeMappingInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
        public SamlIdpAttributeMappingInfo(string name, string type, string value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }

    public class SamlIdpGroupsMappingInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("filterType")]
        public string FilterType { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
        [JsonPropertyName("roles")]
        public List<SamlIdpRoleGroupMappingInfo> Roles { get; set; }
        public SamlIdpGroupsMappingInfo(string name, string type, string filterType, string value, List<SamlIdpRoleGroupMappingInfo> roles)
        {
            Name = name;
            Type = type;
            FilterType = filterType;
            Value = value;
            Roles = roles;
        }
    }

    public class SamlIdpRoleGroupMappingInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        public SamlIdpRoleGroupMappingInfo(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class SsoApplicationResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        [JsonPropertyName("logo")]
        public string? Logo { get; set; }
        [JsonPropertyName("appType")]
        public string AppType { get; set; }
        [JsonPropertyName("samlSettings")]
        public SsoApplicationSamlSettings? SamlSettings { get; set; }
        [JsonPropertyName("oidcSettings")]
        public SsoApplicationOidcSettings? OidcSettings { get; set; }
        public SsoApplicationResponse(string id, string name, string appType)
        {
            Id = id;
            Name = name;
            AppType = appType;
        }
    }

    public class SsoApplicationSamlSettings
    {
        [JsonPropertyName("loginPageUrl")]
        public string LoginPageUrl { get; set; }
        [JsonPropertyName("idpCert")]
        public string? IdpCert { get; set; }
        [JsonPropertyName("useMetadataInfo")]
        public bool UseMetadataInfo { get; set; }
        [JsonPropertyName("metadataUrl")]
        public string? MetadataUrl { get; set; }
        [JsonPropertyName("entityId")]
        public string? EntityId { get; set; }
        [JsonPropertyName("acsUrl")]
        public string? AcsUrl { get; set; }
        [JsonPropertyName("certificate")]
        public string? Certificate { get; set; }
        [JsonPropertyName("attributeMapping")]
        public List<SamlIdpAttributeMappingInfo>? AttributeMapping { get; set; }
        [JsonPropertyName("groupsMapping")]
        public List<SamlIdpGroupsMappingInfo>? GroupsMapping { get; set; }
        [JsonPropertyName("idpMetadataUrl")]
        public string? IdpMetadataUrl { get; set; }
        [JsonPropertyName("idpEntityId")]
        public string? IdpEntityId { get; set; }
        [JsonPropertyName("idpSsoUrl")]
        public string? IdpSsoUrl { get; set; }
        [JsonPropertyName("acsAllowedCallbacks")]
        public List<string>? AcsAllowedCallbacks { get; set; }
        [JsonPropertyName("subjectNameIdType")]
        public string? SubjectNameIdType { get; set; }
        [JsonPropertyName("subjectNameIdFormat")]
        public string? SubjectNameIdFormat { get; set; }
        public SsoApplicationSamlSettings(string loginPageUrl)
        {
            LoginPageUrl = loginPageUrl;
        }
    }

    public class SsoApplicationOidcSettings
    {
        [JsonPropertyName("loginPageUrl")]
        public string LoginPageUrl { get; set; }
        [JsonPropertyName("issuer")]
        public string Issuer { get; set; }
        [JsonPropertyName("discoveryUrl")]
        public string DiscoveryUrl { get; set; }
        public SsoApplicationOidcSettings(string loginPageUrl, string issuer, string discoveryUrl)
        {
            LoginPageUrl = loginPageUrl;
            Issuer = issuer;
            DiscoveryUrl = discoveryUrl;
        }
    }
}
