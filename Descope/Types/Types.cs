
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

    /// <summary>
    /// Used to require additional behaviors when authenticating a user.
    /// </summary>
    public class LoginOptions
    {
        /// <summary>
        /// Used to add layered security to your app by implementing Step-up authentication.
        /// <para>
        /// After the Step-up authentication completes successfully the returned session JWT will
        /// have an `su` claim with a value of `true`.
        /// </para>
        /// <para>
        /// <b>Note:</b> The <c>su</c> claim is not set on the refresh JWT.
        /// </para>
        /// </summary>
        public string? StepupRefreshJwt { get; set; }
        /// <summary>
        /// Used to add layered security to your app by implementing Multi-factor authentication.
        /// <para>
        /// Assuming the user has already signed in successfully with one authentication method,
        /// we can take the <c>RefreshJwt</c> from the <c>AuthenticationResponse</c> and pass it as the
        /// Refresh JWT value to another authentication method.
        /// </para>
        /// <para>
        /// After the MFA authentication completes successfully the <c>amr</c> claim in both the session
        /// and refresh JWTs will be an array with an entry for each authentication method used.
        /// </para>
        /// </summary>
        public string? MfaRefreshJwt { get; set; }
        /// <summary>
        /// Adds additional custom claims to the user's JWT during authentication.
        /// <para>
        /// <b>Important:</b> Any custom claims added via this method are considered insecure and will
        /// be nested under the <c>nsec</c> custom claim.
        /// </para>
        /// </summary>
        public Dictionary<string, object>? CustomClaims { get; set; }

        public bool IsJWTRequired => StepupRefreshJwt != null || MfaRefreshJwt != null;
    }

    /// <summary>
    /// Used to require additional behaviors when signing up and then authenticating a user.
    /// </summary>
    public class SignUpOptions
    {
        /// <summary>
        /// Optional custom signup template ID
        /// </summary>
        [JsonPropertyName("templateId")]
        public string? TemplateID { get; set; }
        /// <summary>
        /// Optional custom signup template key-value options
        /// </summary>
        [JsonPropertyName("templateOptions")]
        public Dictionary<string, string>? TemplateOptions { get; set; }
        /// <summary>
        /// Adds additional custom claims to the user's JWT during authentication.
        /// <para>
        /// <b>Important:</b> Any custom claims added via this method are considered insecure and will
        /// be nested under the <c>nsec</c> custom claim.
        /// </para>
        /// </summary>
        [JsonPropertyName("customClaims")]
        public Dictionary<string, object>? CustomClaims { get; set; }
    }


    public class UpdateOptions
    {
        public bool AddToLoginIds { get; set; } = false;
        public bool OnMergeUseExisting { get; set; } = false;
    }

    public class AuthenticationResponse
    {
        [JsonPropertyName("sessionJwt")]
        public string SessionJwt { get; set; }

        [JsonPropertyName("refreshJwt")]
        public string? RefreshJwt { get; set; }

        [JsonPropertyName("cookieDomain")]
        public string CookieDomain { get; set; }

        [JsonPropertyName("cookiePath")]
        public string CookiePath { get; set; }

        [JsonPropertyName("cookieMaxAge")]
        public int CookieMaxAge { get; set; }

        [JsonPropertyName("cookieExpiration")]
        public int CookieExpiration { get; set; }

        [JsonPropertyName("user")]
        public UserResponse User { get; set; }

        [JsonPropertyName("firstSeen")]
        public bool FirstSeen { get; set; }
        public AuthenticationResponse(string sessionJwt, string? refreshJwt, string cookieDomain, string cookiePath, int cookieMaxAge, int cookieExpiration, UserResponse user, bool firstSeen)
        {
            SessionJwt = sessionJwt;
            RefreshJwt = refreshJwt;
            CookieDomain = cookieDomain;
            CookiePath = cookiePath;
            CookieMaxAge = cookieMaxAge;
            CookieExpiration = cookieExpiration;
            User = user;
            FirstSeen = firstSeen;
        }
    }

    public class Session
    {
        public Token SessionToken { get; set; }
        public Token RefreshToken { get; set; }
        public UserResponse User { get; set; }
        public bool FirstSeen { get; set; }
        public Session(Token sessionToken, Token refreshToken, UserResponse user, bool firstSeen)
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
            
            // Decode the JWT payload from the encoded token
            // JWT format is: header.payload.signature
            var tokenParts = jsonWebToken.EncodedToken.Split('.');
            if (tokenParts.Length >= 2)
            {
                // Decode the payload (second part)
                var payloadJson = Base64UrlDecode(tokenParts[1]);
                var payload = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(payloadJson);
                Claims = new Dictionary<string, object>();
                if (payload != null)
                {
                    foreach (var kvp in payload)
                    {
                        Claims[kvp.Key] = ConvertJsonElement(kvp.Value);
                    }
                }
            }
            else
            {
                // Fallback to the old behavior if token format is unexpected
                Claims = new Dictionary<string, object>();
                foreach (var claim in jsonWebToken.Claims)
                {
                    Claims[claim.Type] = claim.Value;
                }
            }
        }
        
        private static string Base64UrlDecode(string base64Url)
        {
            // Convert base64url to base64
            var base64 = base64Url.Replace('-', '+').Replace('_', '/');
            // Add padding if needed
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        
        private static object ConvertJsonElement(System.Text.Json.JsonElement element)
        {
            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.String:
                    return element.GetString() ?? string.Empty;
                case System.Text.Json.JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    return element.GetDouble();
                case System.Text.Json.JsonValueKind.True:
                    return true;
                case System.Text.Json.JsonValueKind.False:
                    return false;
                case System.Text.Json.JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(item));
                    }
                    return list;
                case System.Text.Json.JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dict[property.Name] = ConvertJsonElement(property.Value);
                    }
                    return dict;
                case System.Text.Json.JsonValueKind.Null:
                    // Return null for null JSON values - consuming code should handle null checks
                    return null!;
                default:
                    // For any unexpected value types, convert to string
                    return element.ToString() ?? string.Empty;
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
        Email, Sms, Whatsapp
    }

    public enum UserStatus
    {
        Enabled,
        Disabled,
        Invited
    }

    public static class UserStatusExtensions
    {
        public static string ToStringValue(this UserStatus status)
        {
            return status switch
            {
                UserStatus.Enabled => "enabled",
                UserStatus.Disabled => "disabled",
                UserStatus.Invited => "invited",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }

    public class UserResponse
    {
        [JsonPropertyName("loginIds")]
        public List<string> LoginIds { get; set; }
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }
        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }
        [JsonPropertyName("familyName")]
        public string? FamilyName { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
        [JsonPropertyName("verifiedEmail")]
        public bool VerifiedEmail { get; set; }
        [JsonPropertyName("verifiedPhone")]
        public bool VerifiedPhone { get; set; }
        [JsonPropertyName("roleNames")]
        public List<string>? RoleNames { get; set; }
        [JsonPropertyName("userTenants")]
        public List<AssociatedTenant>? UserTenants { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
        [JsonPropertyName("test")]
        public bool Test { get; set; }
        [JsonPropertyName("customAttributes")]
        public Dictionary<string, object>? CustomAttributes { get; set; }
        [JsonPropertyName("createdTime")]
        public int CreatedTime { get; set; }
        [JsonPropertyName("totp")]
        public bool Totp { get; set; }
        [JsonPropertyName("webauthn")]
        public bool Webauthn { get; set; }
        [JsonPropertyName("password")]
        public bool Password { get; set; }
        [JsonPropertyName("saml")]
        public bool Saml { get; set; }
        [JsonPropertyName("oauth")]
        public Dictionary<string, object>? oauth { get; set; }
        [JsonPropertyName("ssoAppIds")]
        public List<string>? SsoAppIds { get; set; }
        public UserResponse(List<string> loginIds, string userId, string status)
        {
            LoginIds = loginIds;
            UserId = userId;
            Status = status;
        }
    }

    public class BatchCreateUserResponse
    {
        [JsonPropertyName("createdUsers")]
        public List<UserResponse> CreatedUsers { get; set; }

        [JsonPropertyName("failedUsers")]
        public List<UsersFailedResponse> FailedUsers { get; set; }

        public BatchCreateUserResponse(List<UserResponse> createdUsers, List<UsersFailedResponse> failedUsers)
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
        public UserResponse User { get; set; }

        public UsersFailedResponse(string failure, UserResponse user)
        {
            Failure = failure;
            User = user;
        }
    }

    public class UserRequest
    {
        public string? Name { get; set; }
        public string? GivenName { get; set; }
        public string? MiddleName { get; set; }
        public string? FamilyName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public List<string>? RoleNames { get; set; }
        public List<AssociatedTenant>? UserTenants { get; set; }
        public Dictionary<string, object>? CustomAttributes { get; set; }
        public string? Picture { get; set; }
        public bool VerifiedEmail { get; set; }
        public bool VerifiedPhone { get; set; }
        public List<string>? AdditionalLoginIds { get; set; }
        public List<string>? SsoAppIds { get; set; }
    }

    public class BatchUser : UserRequest
    {
        public string LoginId { get; set; }

        public BatchUserPassword? Password { get; set; }

        public UserStatus? Status { get; set; }

        public BatchUser(string loginId)
        {
            LoginId = loginId;
        }
    }

    public class BatchUserPassword
    {
        public string? Cleartext { get; set; }
        public BatchUserPasswordHashed? Hashed { get; set; }
    }

    public class BatchUserPasswordHashed
    {
        public BatchUserPasswordBcrypt? Bcrypt { get; set; }
        public BatchUserPasswordFirebase? Firebase { get; set; }
        public BatchUserPasswordPbkdf2? Pbkdf2 { get; set; }
        public BatchUserPasswordDjango? Django { get; set; }
    }

    public class BatchUserPasswordBcrypt
    {
        public string Hash { get; set; }

        public BatchUserPasswordBcrypt(string hash)
        {
            Hash = hash;
        }
    }

    public class BatchUserPasswordFirebase
    {
        public byte[] Hash { get; set; } // the hash in raw bytes (base64 strings should be decoded first)
        public byte[] Salt { get; set; } // the salt in raw bytes (base64 strings should be decoded first)
        public byte[] SaltSeparator { get; set; } // the salt separator (usually 1 byte long)
        public byte[] SignerKey { get; set; } // the signer key (base64 strings should be decoded first)
        public int Memory { get; set; } // the memory cost value (usually between 12 to 17)
        public int Rounds { get; init; } // the rounds cost value (usually between 6 to 10)

        public BatchUserPasswordFirebase(byte[] hash, byte[] salt, byte[] saltSeparator, byte[] signerKey, int memory, int rounds)
        {
            Hash = hash;
            Salt = salt;
            SaltSeparator = saltSeparator;
            SignerKey = signerKey;
            Memory = memory;
            Rounds = rounds;
        }
    }

    public class BatchUserPasswordPbkdf2
    {
        public byte[] Hash { get; set; } // the hash in raw bytes (base64 strings should be decoded first)
        public byte[] Salt { get; set; } // the salt in raw bytes (base64 strings should be decoded first)
        public int Iterations { get; set; } // the iterations cost value (usually in the thousands)
        public string Type { get; set; } // the hash name (sha1, sha256, sha512)
        public BatchUserPasswordPbkdf2(byte[] hash, byte[] salt, int iterations, string type)
        {
            Hash = hash;
            Salt = salt;
            Iterations = iterations;
            Type = type;
        }
    }

    public class BatchUserPasswordDjango
    {
        public string Hash { get; set; }

        public BatchUserPasswordDjango(string hash)
        {
            Hash = hash;
        }
    }

    public class InviteOptions
    {
        public string? InviteUrl { get; set; }
        public bool SendMail { get; set; } // send invite via mail, default is according to project settings
        public bool SendSms { get; set; } // send invite via text message, default is according to project settings
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
        public List<string>? TenantIds { get; set; }
        [JsonPropertyName("ssoAppIDs")]
        public List<string>? SsoAppIds { get; set; }
        [JsonPropertyName("customAttributes")]
        public Dictionary<string, object>? CustomAttributes { get; set; }
        [JsonPropertyName("withTestUsers")]
        public bool WithTestUsers { get; set; }
        [JsonPropertyName("testUsersOnly")]
        public bool TestUsersOnly { get; set; }
        [JsonPropertyName("tenantRoleIds")]
        public Dictionary<string, List<string>>? TenantRoleIds { get; set; }
        [JsonPropertyName("tenantRoleNames")]
        public Dictionary<string, List<string>>? TenantRoleNames { get; set; }
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
        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; }
        [JsonPropertyName("tenantName")]
        public string? TenantName { get; set; }
        [JsonPropertyName("roleNames")]
        public List<string>? RoleNames { get; set; }
        public AssociatedTenant(string tenantId)
        {
            TenantId = tenantId;
        }
    }

    public class TenantResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("selfProvisioningDomains")]
        public List<string> SelfProvisioningDomains { get; set; }
        [JsonPropertyName("customAttributes")]
        public Dictionary<string, object> CustomAttributes { get; set; }
        [JsonPropertyName("authType")]
        public string AuthType { get; set; }
        [JsonPropertyName("parent")]
        public string Parent { get; set; }
        [JsonPropertyName("successors")]
        public List<string> Successors { get; set; }
        [JsonPropertyName("domains")]
        public List<string> Domains { get; set; }

        public TenantResponse(string id, string name, List<string>? selfProvisioningDomains, Dictionary<string, object>? customAttributes, string authType, string? parent, List<string>? successors, List<string>? domains)
        {
            Id = id;
            Name = name;
            SelfProvisioningDomains = selfProvisioningDomains ?? new List<string>();
            CustomAttributes = customAttributes ?? new Dictionary<string, object>();
            AuthType = authType;
            Parent = parent ?? string.Empty;
            Successors = successors ?? new List<string>();
            Domains = domains ?? new List<string>();
        }
    }

    public class TenantOptions
    {
        public string Name { get; set; }
        public List<string>? SelfProvisioningDomains { get; set; }
        public Dictionary<string, object>? CustomAttributes { get; set; }
        public string? Parent { get; set; }
        public TenantOptions(string name)
        {
            Name = name;
        }
    }

    public class TenantSearchOptions
    {
        public List<string>? Ids { get; set; }
        public List<string>? Names { get; set; }
        public List<string>? SelfProvisioningDomains { get; set; }
        public Dictionary<string, object>? CustomAttributes { get; set; }
        public string? AuthType { get; set; }
    }

    public class ProviderTokenResponse
    {
        [JsonPropertyName("provider")]
        public string Provider { get; set; }
        [JsonPropertyName("providerUserID")]
        public string ProviderUserID { get; set; }
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }
        [JsonPropertyName("expiration")]
        public int Expiration { get; set; }
        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; }
        public ProviderTokenResponse(string provider, string providerUserID, string accessToken, int expiration, List<string> scopes)
        {
            Provider = provider;
            ProviderUserID = providerUserID;
            AccessToken = accessToken;
            Expiration = expiration;
            Scopes = scopes;
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

    public class AccessKeyLoginOptions
    {
        [JsonPropertyName("customClaims")]
        public Dictionary<string, object>? CustomClaims { get; set; }
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

    public class PermissionResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        public PermissionResponse(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }
    }

    public class RoleResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("permissionNames")]
        public List<string> PermissionNames { get; set; }
        [JsonPropertyName("createdTime")]
        public int CreatedTime { get; set; }
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        public RoleResponse(string name, int createdTime, string? description = null, List<string>? permissionNames = null, string? tenantId = null)
        {
            Name = name;
            Description = description;
            PermissionNames ??= new List<string>();
            CreatedTime = createdTime;
            TenantId = tenantId;
        }
    }

    public class RoleSearchOptions
    {
        [JsonPropertyName("tenantIds")]
        public List<string>? TenantIds { get; set; }
        [JsonPropertyName("roleNames")]
        public List<string>? RoleNames { get; set; }
        [JsonPropertyName("roleNameLike")]
        public string? RoleNameLike { get; set; }
        [JsonPropertyName("permissionNames")]
        public List<string>? PermissionNames { get; set; }
    }

    public class PasswordSettings
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
        [JsonPropertyName("minLength")]
        public int MinLength { get; set; }
        [JsonPropertyName("lowercase")]
        public bool Lowercase { get; set; }
        [JsonPropertyName("uppercase")]
        public bool Uppercase { get; set; }
        [JsonPropertyName("number")]
        public bool Number { get; set; }
        [JsonPropertyName("nonAlphanumeric")]
        public bool NonAlphanumeric { get; set; }
        [JsonPropertyName("expiration")]
        public bool Expiration { get; set; }
        [JsonPropertyName("expirationWeeks")]
        public int ExpirationWeeks { get; set; }
        [JsonPropertyName("reuse")]
        public bool Reuse { get; set; }
        [JsonPropertyName("reuseAmount")]
        public int ReuseAmount { get; set; }
        [JsonPropertyName("lock")]
        public bool Lock { get; set; }
        [JsonPropertyName("lockAttempts")]
        public int LockAttempts { get; set; }
    }

    public class RoleMapping
    {
        public List<string> Groups { get; set; }
        public string Role { get; set; }
        public RoleMapping(List<string> groups, string role)
        {
            Groups = groups;
            Role = role;
        }
    }

    public class SsoSamlSettings
    {
        [JsonPropertyName("idpUrl")]
        public string IdpUrl { get; set; }
        [JsonPropertyName("idpEntityId")]
        public string IdpEntityId { get; set; }
        [JsonPropertyName("idpCert")]
        public string IdpCertificate { get; set; }
        [JsonPropertyName("attributeMapping")]
        public AttributeMapping? AttributeMapping { get; set; }
        [JsonPropertyName("roleMappings")]
        public List<RoleMapping>? RoleMappings { get; set; }
        public SsoSamlSettings(string idpUrl, string idpEntityId, string idpCertificate)
        {
            IdpUrl = idpUrl;
            IdpEntityId = idpEntityId;
            IdpCertificate = idpCertificate;
        }
    }

    public class SsoSamlSettingsByMetadata
    {
        [JsonPropertyName("idpMetadataUrl")]
        public string IdpMetadataUrl { get; set; }
        [JsonPropertyName("attributeMapping")]
        public AttributeMapping? AttributeMapping { get; set; }
        [JsonPropertyName("roleMappings")]
        public List<RoleMapping>? RoleMappings { get; set; }
        public SsoSamlSettingsByMetadata(string idpMetadataUrl)
        {
            IdpMetadataUrl = idpMetadataUrl;
        }
    }

    public class RoleItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        public RoleItem(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class GroupsMapping
    {
        [JsonPropertyName("role")]
        public RoleItem? Role { get; set; }
        [JsonPropertyName("groups")]
        public List<string>? Groups { get; set; }
    }

    // Represents a SAML mapping between Descope and IDP user attributes
    public class AttributeMapping
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }
        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }
        [JsonPropertyName("familyName")]
        public string? FamilyName { get; set; }
        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }
        [JsonPropertyName("group")]
        public string? Group { get; set; }
        [JsonPropertyName("customAttributes")]
        public Dictionary<string, string>? CustomAttributes { get; set; }
    }

    public class SsoSamlSettingsResponse
    {
        [JsonPropertyName("idpEntityId")]
        public string? IdpEntityId { get; set; }
        [JsonPropertyName("idpSSOUrl")]
        public string? IdpSsoUrl { get; set; }
        [JsonPropertyName("idpCertificate")]
        public string? IdpCertificate { get; set; }
        [JsonPropertyName("idpMetadataUrl")]
        public string? IdpMetadataUrl { get; set; }
        [JsonPropertyName("spEntityId")]
        public string? SpEntityId { get; set; }
        [JsonPropertyName("spACSUrl")]
        public string? SpAcsUrl { get; set; }
        [JsonPropertyName("spCertificate")]
        public string? SpCertificate { get; set; }
        [JsonPropertyName("attributeMapping")]
        public AttributeMapping? AttributeMapping { get; set; }
        [JsonPropertyName("groupsMapping")]
        public List<GroupsMapping>? GroupsMapping { get; set; }
        [JsonPropertyName("redirectUrl")]
        public string? RedirectUrl { get; set; }
    }

    public class OidcAttributeMapping
    {
        [JsonPropertyName("loginId")]
        public string? LoginID { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("givenName")]
        public string? GivenName { get; set; }
        [JsonPropertyName("middleName")]
        public string? MiddleName { get; set; }
        [JsonPropertyName("familyName")]
        public string? FamilyName { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("verifiedEmail")]
        public string? VerifiedEmail { get; set; }
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }
        [JsonPropertyName("verifiedPhone")]
        public string? VerifiedPhone { get; set; }
        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

    public class SsoOidcSettings
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("clientId")]
        public string? ClientId { get; set; }
        [JsonPropertyName("clientSecret")]
        public string? ClientSecret { get; set; } // will be empty on response
        [JsonPropertyName("redirectUrl")]
        public string? RedirectUrl { get; set; }
        [JsonPropertyName("authUrl")]
        public string? AuthUrl { get; set; }
        [JsonPropertyName("tokenUrl")]
        public string? TokenUrl { get; set; }
        [JsonPropertyName("userDataUrl")]
        public string? UserDataUrl { get; set; }
        [JsonPropertyName("scope")]
        public List<string>? Scope { get; set; }
        [JsonPropertyName("JWKsUrl")]
        public string? JwksUrl { get; set; }
        [JsonPropertyName("userAttrMapping")]
        public OidcAttributeMapping? AttributeMapping { get; set; }
        [JsonPropertyName("manageProviderTokens")]
        public bool ManageProviderTokens { get; set; }
        [JsonPropertyName("callbackDomain")]
        public string? CallbackDomain { get; set; }
        [JsonPropertyName("prompt")]
        public List<string>? Prompt { get; set; }
        [JsonPropertyName("grantType")]
        public string? GrantType { get; set; }
        [JsonPropertyName("issuer")]
        public string? Issuer { get; set; }
    }

    public class SsoTenantSettings
    {
        [JsonPropertyName("tenant")]
        public TenantResponse Tenant { get; set; }
        [JsonPropertyName("saml")]
        public SsoSamlSettingsResponse Saml { get; set; }
        [JsonPropertyName("oidc")]
        public SsoOidcSettings Oidc { get; set; }
        public SsoTenantSettings(TenantResponse tenant, SsoSamlSettingsResponse saml, SsoOidcSettings oidc)
        {
            Tenant = tenant;
            Saml = saml;
            Oidc = oidc;
        }
    }


    public class EnchantedLinkResponse
    {
        // Pending referral code used to poll enchanted link authentication status
        [JsonPropertyName("pendingRef")]
        public string PendingRef { get; set; }
        // Link id, on which link the user should click
        [JsonPropertyName("linkId")]
        public string LinkId { get; set; }
        // Masked email to which the email was sent
        [JsonPropertyName("maskedEmail")]
        public string MaskedEmail { get; set; }


        public EnchantedLinkResponse(string pendingRef, string linkId, string maskedEmail)
        {
            PendingRef = pendingRef;
            LinkId = linkId;
            MaskedEmail = maskedEmail;
        }
    }

}
