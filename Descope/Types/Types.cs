
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Descope
{

    public class SignUpDetails
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }
        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }
        [JsonPropertyName("familyName")]
        public string? FamilyName { get; set; }
    }

    public class UpdateOptions
    {
        public bool AddToLoginIds { get; set; } = false;
        public bool OnMergeUseExisting { get; set; } = false;
    }

    public class AuthenticationResponse
    {
        [JsonPropertyName("sessionJwt")]
        public string sessionJwt { get; set; }

        [JsonPropertyName("refreshJwt")]
        public string? refreshJwt { get; set; }

        [JsonPropertyName("cookieDomain")]
        public string cookieDomain { get; set; }

        [JsonPropertyName("cookiePath")]
        public string cookiePath { get; set; }

        [JsonPropertyName("cookieMaxAge")]
        public int cookieMaxAge { get; set; }

        [JsonPropertyName("cookieExpiration")]
        public int cookieExpiration { get; set; }

        [JsonPropertyName("user")]
        public DescopeUser user { get; set; }

        [JsonPropertyName("firstSeen")]
        public bool firstSeen { get; set; }
    }

    public class Session
    {
        public Token SessionToken { get; set; }
        public Token RefreshToken { get; set; }
        public DescopeUser User { get; set; }
        public bool FirstSeen { get; set; }
        public Session(Token sessionToken, Token refreshToken, DescopeUser user, bool firstSeen)
        {
            SessionToken = sessionToken;
            RefreshToken = refreshToken;
            User = user;
            FirstSeen = firstSeen;
        }
    }

    public class Token
    {
        [JsonPropertyName("jwt")]
        public string Jwt { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }
        [JsonPropertyName("expiration")]
        public DateTime Expiration { get; set; }
        [JsonPropertyName("claims")]
        public Dictionary<string, object> Claims { get; set; }
        [JsonPropertyName("refreshExpiration")]
        public DateTime? RefreshExpiration { get; set; }
        public Token(string jwt, string id, string projectId, DateTime expiration, Dictionary<string, object> claims, DateTime? refreshExpiration = null)
        {
            Jwt = jwt;
            Id = id;
            ProjectId = projectId;
            Expiration = expiration;
            Claims = claims;
            RefreshExpiration = refreshExpiration;
        }
        public Token(JsonWebToken jsonWebToken)
        {
            Jwt = jsonWebToken.EncodedToken;
            Id = jsonWebToken.Subject;
            Expiration = jsonWebToken.ValidTo;
            var parts = jsonWebToken.Issuer.Split("/");
            ProjectId = parts.Last();
            Claims = new Dictionary<string, object>();
            foreach (var claim in jsonWebToken.Claims)
            {
                Claims[claim.Type] = claim.Value;
            }
        }

        internal List<string> GetTenants()
        {
            return new List<string>(GetTenantsClaim().Keys);
        }

        internal object? GetTenantValue(string tenant, string key)
        {
            return (GetTenantsClaim()[tenant] is Dictionary<string, object> info) ? info[key] : null;
        }

        private Dictionary<string, object> GetTenantsClaim()
        {
            return Claims["tenants"] as Dictionary<string, object> ?? new Dictionary<string, object>();
        }
    }

    public enum DeliveryMethod
    {
        email,
        sms,
        whatsapp
    }

    public class LoginOptions
    {
        [JsonPropertyName("stepup")]
        public bool stepup { get; set; }

        [JsonPropertyName("customClaims")]
        public Dictionary<string, object> customClaims { get; set; }

        [JsonPropertyName("mfa")]
        public bool mfa { get; set; }
    }

    public class DescopeUser
    {
        public List<string> loginIds { get; set; }
        public string userId { get; set; }
        public string name { get; set; }
        public string givenName { get; set; }
        public string middleName { get; set; }
        public string familyName { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public bool verifiedEmail { get; set; }
        public bool verifiedPhone { get; set; }
        public List<string> roleNames { get; set; }
        public List<AssociatedTenant> userTenants { get; set; }
        public string status { get; set; }
        public string picture { get; set; }
        public bool test { get; set; }
        public Dictionary<string, object> customAttributes { get; set; }
        public int createdTime { get; set; }
        public bool totp { get; set; }
        public bool webauthn { get; set; }
        public bool password { get; set; }
        public bool saml { get; set; }
        public Dictionary<string, object> oauth { get; set; }
        public List<string> ssoAppIds { get; set; }
    }

    public class BatchCreateUserResponse
    {
        [JsonPropertyName("createdUsers")]
        public List<DescopeUser> CreatedUsers { get; set; }

        [JsonPropertyName("failedUsers")]
        public List<UsersFailedResponse> FailedUsers { get; set; }

        public BatchCreateUserResponse(List<DescopeUser> createdUsers, List<UsersFailedResponse> failedUsers)
        {
            CreatedUsers = createdUsers;
            FailedUsers = failedUsers;
        }

    }

    public class UsersFailedResponse
    {
        [JsonPropertyName("failure")]
        public string Failure { get; set; }
        [JsonPropertyName("user")]
        public DescopeUser User { get; set; }

        public UsersFailedResponse(string failure, DescopeUser user)
        {
            Failure = failure;
            User = user;
        }
    }

    public class UserRequest
    {
        public string name { get; set; }
        public string givenName { get; set; }
        public string middleName { get; set; }
        public string familyName { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public List<string> roleNames { get; set; }
        public List<AssociatedTenant> userTenants { get; set; }
        public Dictionary<string, object> customAttributes { get; set; }
        public string picture { get; set; }
        public bool verifiedEmail { get; set; }
        public bool verifiedPhone { get; set; }
        public List<string> additionalLoginIds { get; set; }
        public List<string> ssoAppIds { get; set; }
    }

    public class BatchUser : UserRequest
    {
        public string loginId { get; set; }
        public BatchUserPassword password { get; set; }
    }

    public class BatchUserPassword
    {
        public string cleartext { get; set; }
        public BatchUserPasswordHashed hashed { get; set; }
    }

    public class BatchUserPasswordHashed
    {
        public BatchUserPasswordBcrypt bcrypt { get; set; }
        public BatchUserPasswordFirebase firebase { get; set; }
        public BatchUserPasswordPbkdf2 pbkdf2 { get; set; }
        public BatchUserPasswordDjango django { get; set; }
    }

    public class BatchUserPasswordBcrypt
    {
        public string hash { get; set; }
    }

    public class BatchUserPasswordFirebase
    {
        public byte[] hash { get; set; } // the hash in raw bytes (base64 strings should be decoded first)
        public byte[] salt { get; set; } // the salt in raw bytes (base64 strings should be decoded first)
        public byte[] saltSeparator { get; set; } // the salt separator (usually 1 byte long)
        public byte[] signerKey { get; set; } // the signer key (base64 strings should be decoded first)
        public int memory { get; set; } // the memory cost value (usually between 12 to 17)
        public int rounds { get; init; } // the rounds cost value (usually between 6 to 10)
    }

    public class BatchUserPasswordPbkdf2
    {
        public byte[] hash { get; set; } // the hash in raw bytes (base64 strings should be decoded first)
        public byte[] salt { get; set; } // the salt in raw bytes (base64 strings should be decoded first)
        public int iterations { get; set; } // the iterations cost value (usually in the thousands)
        public string type { get; set; } // the hash name (sha1, sha256, sha512)
    }

    public class BatchUserPasswordDjango
    {
        public string hash { get; set; }
    }

    public class InviteOptions
    {
        public string inviteUrl { get; set; }
        public bool sendMail { get; set; } // send invite via mail, default is according to project settings
        public bool sendSms { get; set; } // send invite via text message, default is according to project settings
    }

    // Options for searching and filtering users
    //
    // Limit - limits the number of returned users. Leave at 0 to return the default amount.
    // Page - allows to paginate over the results. Pages start at 0 and must non-negative.
    // Sort - allows to sort by fields.
    // Text - allows free text search among all user's attributes.
    // TenantIDs - filter by tenant IDs.
    // Roles - filter by role names.
    // CustomAttributes map is an optional filter for custom attributes:
    // where the keys are the attribute names and the values are either a value we are searching for or list of these values in a slice.
    // We currently support string, int and bool values
    public class SearchUserOptions
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }
        [JsonPropertyName("limit")]
        public int Limit { get; set; }
        [JsonPropertyName("sort")]
        public List<UserSearchSort>? Sort;
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        [JsonPropertyName("emails")]
        public List<string>? Emails { get; set; }
        [JsonPropertyName("phones")]
        public List<string>? Phones { get; set; }
        [JsonPropertyName("statuses")]
        public List<string>? Statuses { get; set; }
        [JsonPropertyName("roles")]
        public List<string>? Roles { get; set; }
        [JsonPropertyName("tenantIds")]
        public List<string>? TenantIDs { get; set; }
        [JsonPropertyName("ssoAppIDs")]
        public List<string>? SsoAppIds { get; set; }
        [JsonPropertyName("customAttributes")]
        public Dictionary<string, object>? CustomAttributes { get; set; }
        [JsonPropertyName("withTestUsers")]
        public bool WithTestUsers { get; set; }
        [JsonPropertyName("testUsersOnly")]
        public bool TestUsersOnly { get; set; }
    }

    public class UserSearchSort
    {
        [JsonPropertyName("field")]
        public string Field { get; set; }
        [JsonPropertyName("desc")]
        public bool Desc { get; set; }
        public UserSearchSort(string field, bool desc)
        {
            Field = field;
            Desc = desc;
        }
    }

    public class UserTestOTPResponse
    {
        [JsonPropertyName("loginId")]
        public string LoginId { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        public UserTestOTPResponse(string loginId, string code)
        {
            LoginId = loginId;
            Code = code;
        }
    }

    public class UserTestMagicLinkResponse
    {
        [JsonPropertyName("loginId")]
        public string LoginId { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        public UserTestMagicLinkResponse(string loginId, string link)
        {
            LoginId = loginId;
            Link = link;
        }
    }

    public class UserTestEnchantedLinkResponse
    {
        [JsonPropertyName("loginId")]
        public string LoginId { get; set; }

        [JsonPropertyName("link")]
        public string Link { get; set; }

        [JsonPropertyName("pendingRef")]
        public string PendingRef { get; set; }

        public UserTestEnchantedLinkResponse(string loginId, string link, string pendingRef)
        {
            LoginId = loginId;
            Link = link;
            PendingRef = pendingRef;
        }
    }

    // Represents a tenant association for a User or an Access Key. The tenant ID is required
    // to denote which tenant the user / access key belongs to. Roles is an optional list of
    // roles for the user / access key in this specific tenant.
    public class AssociatedTenant
    {
        public string tenantId { get; set; }
        public string tenantName { get; set; }
        public List<string> roleNames { get; set; }
    }

    public class TenantResponse
    {
        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("selfProvisioningDomains")]
        public List<string> selfProvisioningDomains { get; set; }

        [JsonPropertyName("customAttributes")]
        public Dictionary<string, object> customAttributes { get; set; }

        public TenantResponse(string Id, string Name, List<string> SelfProvisioningDomains = null, Dictionary<string, object> CustomAttributes = null)
        {
            id = Id;
            name = Name;
            selfProvisioningDomains = SelfProvisioningDomains;
            customAttributes = CustomAttributes;
        }
    }

    public class TenantOptions
    {
        public string name;
        public List<string>? selfProvisioningDomains;
        public Dictionary<string, object>? customAttributes;
    }

    public class TenantSearchOptions
    {
        public List<string>? ids;
        public List<string>? names;
        public List<string>? selfProvisioningDomains;
        public Dictionary<string, object>? customAttributes;
        public string? authType;
    }

    public class ProviderTokenResponse
    {
        public string provider;

        public string providerUserID;

        public string accessToken;

        public int expiration;

        public List<string> scopes;

        public ProviderTokenResponse(string provider, string providerUserID, string accessToken, int expiration, List<string> scopes)
        {
            this.provider = provider;
            this.providerUserID = providerUserID;
            this.accessToken = accessToken;
            this.expiration = expiration;
            this.scopes = scopes;
        }
    }

    public class AccessKeyCreateResponse
    {
        [JsonPropertyName("cleartext")]
        public string Cleartext { get; set; }

        [JsonPropertyName("key")]
        public AccessKeyResponse Key { get; set; }

        public AccessKeyCreateResponse(string cleartext, AccessKeyResponse key)
        {
            Cleartext = cleartext;
            Key = key;
        }
    }

    public class AccessKeyResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("roleNames")]
        public List<string> RoleNames { get; set; }

        [JsonPropertyName("keyTenants")]
        public List<AssociatedTenant> KeyTenants { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("createdTime")]
        public int CreatedTime { get; set; }

        [JsonPropertyName("expireTime")]
        public int ExpireTime { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; }

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }

        [JsonPropertyName("boundUserId")]
        public string UserId { get; set; }

        public AccessKeyResponse(string id, string name, List<string> roleNames, List<AssociatedTenant> keyTenants, string status, int createdTime, int expireTime, string createdBy, string clientId, string userId)
        {
            Id = id;
            Name = name;
            RoleNames = roleNames;
            KeyTenants = keyTenants;
            Status = status;
            CreatedTime = createdTime;
            ExpireTime = expireTime;
            CreatedBy = createdBy;
            ClientId = clientId;
            UserId = userId;
        }
    }

    public class ProjectCloneResponse
    {
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; }

        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        public ProjectCloneResponse(string projectId, string projectName, string tag)
        {
            ProjectId = projectId;
            ProjectName = projectName;
            Tag = tag;
        }
    }
}
