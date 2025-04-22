using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Descope.Internal.Auth
{
    public class Authentication : IAuthentication
    {
        public IOtp Otp { get => _otp; }
        public IOAuth OAuth { get => _oauth; }
        public IEnchantedLink EnchantedLink { get => _enchantedLink; }
        public ISsoAuth Sso { get => _sso; }

        private readonly Otp _otp;
        private readonly OAuth _oauth;
        private readonly EnchantedLink _enchantedLink;
        private readonly Sso _sso;

        private readonly IHttpClient _httpClient;
        private readonly JsonWebTokenHandler _jsonWebTokenHandler = new();
        private readonly Dictionary<string, List<SecurityKey>> _securityKeys = new();

        private const string ClaimPermissions = "permissions";
        private const string ClaimRoles = "roles";

        public Authentication(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _otp = new Otp(httpClient);
            _oauth = new OAuth(httpClient);
            _enchantedLink = new EnchantedLink(httpClient);
            _sso = new Sso(httpClient);
        }

        public async Task<Token> ValidateSession(string sessionJwt)
        {
            if (string.IsNullOrEmpty(sessionJwt)) throw new DescopeException("sessionJwt empty");
            var token = await ValidateToken(sessionJwt) ?? throw new DescopeException("Session invalid");
            return token;
        }

        public async Task<Token> RefreshSession(string refreshJwt)
        {
            if (string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("refreshJwt empty");
            var refreshToken = await ValidateToken(refreshJwt) ?? throw new DescopeException("Refresh token invalid");
            var response = await _httpClient.Post<AuthenticationResponse>(Routes.AuthRefresh, refreshJwt);
            try
            {
                return new Token(_jsonWebTokenHandler.ReadJsonWebToken(response.SessionJwt))
                {
                    RefreshExpiration = refreshToken.Expiration
                };
            }
            catch
            {
                throw new DescopeException("Unable to parse refreshed session jwt");
            }
        }

        public async Task<Token> ValidateAndRefreshSession(string sessionJwt, string refreshJwt)
        {
            if (string.IsNullOrEmpty(sessionJwt) && string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("Both sessionJwt and refreshJwt are empty");
            if (!string.IsNullOrEmpty(sessionJwt))
            {
                try { return await ValidateSession(sessionJwt); }
                catch { }
            }
            if (string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("Cannot refresh session with empty refresh JWT");
            return await RefreshSession(refreshJwt);
        }

        public async Task<Token> ExchangeAccessKey(string accessKey, AccessKeyLoginOptions? loginOptions = null)
        {
            if (string.IsNullOrEmpty(accessKey)) throw new DescopeException("access key missing");
            var response = await _httpClient.Post<AccessKeyExchangeResponse>(Routes.AuthAccessKeyExchange, accessKey, new { loginOptions });
            return new Token(_jsonWebTokenHandler.ReadJsonWebToken(response.SessionJwt)) ?? throw new DescopeException("Failed to parse exchanged token");
        }

        public bool ValidatePermissions(Token token, List<string> permissions, string? tenant)
        {
            return ValidateAgainstClaims(token, ClaimPermissions, permissions, tenant);
        }

        public List<string> GetMatchedPermissions(Token token, List<string> permissions, string? tenant)
        {
            return GetMatchedClaimValues(token, ClaimPermissions, permissions, tenant);
        }

        public bool ValidateRoles(Token token, List<string> roles, string? tenant)
        {
            return ValidateAgainstClaims(token, ClaimRoles, roles, tenant);
        }

        public List<string> GetMatchedRoles(Token token, List<string> roles, string? tenant)
        {
            return GetMatchedClaimValues(token, ClaimRoles, roles, tenant);
        }

        public async Task<Session> SelectTenant(string tenant, string refreshJwt)
        {
            if (string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("refreshJwt empty");
            var token = await ValidateToken(refreshJwt) ?? throw new DescopeException("invalid refreshJwt");
            var body = new { tenant };
            var response = await _httpClient.Post<AuthenticationResponse>(Routes.AuthSelectTenant, refreshJwt, body);
            return AuthResponseToSession(response, token);
        }

        public async Task LogOut(string refreshJwt)
        {
            if (string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("refreshJwt empty");
            _ = await ValidateToken(refreshJwt) ?? throw new DescopeException("invalid refreshJwt");
            await _httpClient.Post<object>(Routes.AuthLogOut, refreshJwt);
        }

        public async Task LogOutAll(string refreshJwt)
        {
            if (string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("refreshJwt empty");
            _ = await ValidateToken(refreshJwt) ?? throw new DescopeException("invalid refreshJwt");
            await _httpClient.Post<object>(Routes.AuthLogOutAll, refreshJwt);
        }

        public async Task<UserResponse> Me(string refreshJwt)
        {
            if (string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("refreshJwt empty");
            _ = await ValidateToken(refreshJwt) ?? throw new DescopeException("invalid refreshJwt");
            return await _httpClient.Get<UserResponse>(Routes.AuthMe, refreshJwt);
        }

        #region Internal

        private async Task<Token?> ValidateToken(string jwt)
        {
            await FetchKeyIfNeeded();
            try
            {
                var token = _jsonWebTokenHandler.ReadJsonWebToken(jwt) ?? throw new System.Exception("Unable to read token");
                var result = await _jsonWebTokenHandler.ValidateTokenAsync(jwt, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) => _securityKeys[kid],
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(5),
                });
                if (result.Exception != null) throw result.Exception;
                return result.IsValid ? new Token(token) : null;
            }
            catch { return null; }
        }

        private async Task FetchKeyIfNeeded()
        {
            if (_securityKeys != null && _securityKeys.Count > 0) return;

            var response = await _httpClient.Get<JwtKeyResponse>(Routes.AuthKeys + $"{_httpClient.DescopeConfig.ProjectId}");
            foreach (var key in response.Keys)
            {
                var rsa = RSA.Create();
                rsa.ImportParameters(key.ToRsaParameters());
                var list = _securityKeys.ContainsKey(key.Kid) ? _securityKeys[key.Kid] : new List<SecurityKey>();
                list.Add(new RsaSecurityKey(rsa));
                _securityKeys[key.Kid] = list;
            }
        }

        private Session AuthResponseToSession(AuthenticationResponse response, Token refreshToken)
        {
            var sessionToken = new Token(_jsonWebTokenHandler.ReadJsonWebToken(response.SessionJwt)) ?? throw new DescopeException("Failed to parse session JWT");
            if (!string.IsNullOrEmpty(response.RefreshJwt))
            {
                refreshToken = new Token(_jsonWebTokenHandler.ReadJsonWebToken(response.RefreshJwt)) ?? throw new DescopeException("Failed to parse refresh JWT");
            }
            sessionToken.RefreshExpiration = refreshToken.Expiration;
            return new Session(sessionToken, refreshToken, response.User, response.FirstSeen);
        }

        private static bool ValidateAgainstClaims(Token token, string claim, List<string> values, string? tenant)
        {
            if (!string.IsNullOrEmpty(tenant) && !IsAssociatedWithTenant(token, tenant)) return false;
            var claimItems = GetAuthorizationClaimItems(token, claim, tenant);
            foreach (var value in values)
            {
                if (!claimItems.Contains(value)) return false;
            }
            return true;
        }

        private static List<string> GetMatchedClaimValues(Token token, string claim, List<string> values, string? tenant)
        {
            if (!string.IsNullOrEmpty(tenant) && !IsAssociatedWithTenant(token, tenant)) return new List<string>();
            var claimItems = GetAuthorizationClaimItems(token, claim, tenant);
            var matched = new List<string>();
            foreach (var value in values)
            {
                if (claimItems.Contains(value)) matched.Add(value);
            }
            return matched;
        }

        private static List<string> GetAuthorizationClaimItems(Token token, string claim, string? tenant)
        {
            if (string.IsNullOrEmpty(tenant))
            {
                if (token.Claims[claim] is List<string> list) return list;
            }
            else
            {
                if (token.GetTenantValue(tenant, claim) is List<string> list) return list;
            }
            return new List<string>();
        }

        private static bool IsAssociatedWithTenant(Token token, string tenant)
        {
            return token.GetTenants().Contains(tenant);
        }

        #endregion Internal
    }

    internal class AccessKeyExchangeResponse
    {
        [JsonPropertyName("sessionJwt")]
        public string SessionJwt { get; set; }

        public AccessKeyExchangeResponse(string sessionJwt)
        {
            SessionJwt = sessionJwt;
        }
    }

    internal class JwtKeyResponse
    {
        [JsonPropertyName("keys")]
        public List<JwtKey> Keys { get; set; }

        public JwtKeyResponse(List<JwtKey> keys)
        {
            Keys = keys;
        }
    }

    public class JwtKey
    {
        [JsonPropertyName("alg")]
        public string Alg { get; set; }

        [JsonPropertyName("e")]
        public string E { get; set; }

        [JsonPropertyName("kid")]
        public string Kid { get; set; }

        [JsonPropertyName("kty")]
        public string Kty { get; set; }

        [JsonPropertyName("n")]
        public string N { get; set; }

        [JsonPropertyName("use")]
        public string Use { get; set; }

        public JwtKey(string alg, string e, string kid, string kty, string n, string use)
        {
            Alg = alg;
            E = e;
            Kid = kid;
            Kty = kty;
            N = n;
            Use = use;
        }

        public RSAParameters ToRsaParameters()
        {
            var modulusBase64 = N;
            modulusBase64 = modulusBase64.Replace("_", "/").Replace("-", "+").PadRight(modulusBase64.Length + (4 - modulusBase64.Length % 4) % 4, '=');
            byte[] modulusBytes = Convert.FromBase64String(modulusBase64);

            var rsaParameters = new RSAParameters
            {
                Modulus = modulusBytes,
                Exponent = Convert.FromBase64String(E)
            };

            return rsaParameters;
        }
    }


}
