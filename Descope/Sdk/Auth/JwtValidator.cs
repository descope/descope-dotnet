using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Descope.Sdk.Auth;

/// <summary>
/// Handles JWT validation using locally cached public keys.
/// </summary>
internal class JwtValidator
{
    private readonly JsonWebTokenHandler _jsonWebTokenHandler = new();
    private readonly Dictionary<string, List<SecurityKey>> _securityKeys = new();
    private readonly string _projectId;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public JwtValidator(string projectId, string baseUrl, HttpClient httpClient)
    {
        _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Validates a JWT token locally using cached public keys.
    /// </summary>
    /// <param name="jwt">The JWT token to validate.</param>
    /// <returns>A validated Token object or null if validation fails.</returns>
    public async Task<Token?> ValidateToken(string jwt)
    {
        if (string.IsNullOrEmpty(jwt)) return null;

        await FetchKeyIfNeeded();

        try
        {
            var token = _jsonWebTokenHandler.ReadJsonWebToken(jwt);
            if (token == null) return null;

            var result = await _jsonWebTokenHandler.ValidateTokenAsync(jwt, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                {
                    if (kid != null && _securityKeys.TryGetValue(kid, out var keys))
                    {
                        return keys;
                    }
                    return new List<SecurityKey>();
                },
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromSeconds(5),
            });

            if (result.Exception != null) return null;
            return result.IsValid ? new Token(token) : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task FetchKeyIfNeeded()
    {
        if (_securityKeys.Count > 0) return;

        var url = $"{_baseUrl.TrimEnd('/')}/v2/keys/{_projectId}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var keyResponse = JsonSerializer.Deserialize<JwtKeyResponse>(content);

        if (keyResponse?.Keys == null) return;

        foreach (var key in keyResponse.Keys)
        {
            var rsa = RSA.Create();
            rsa.ImportParameters(key.ToRsaParameters());

            if (!_securityKeys.ContainsKey(key.Kid))
            {
                _securityKeys[key.Kid] = new List<SecurityKey>();
            }

            _securityKeys[key.Kid].Add(new RsaSecurityKey(rsa));
        }
    }

    internal class JwtKeyResponse
    {
        [JsonPropertyName("keys")]
        public List<JwtKey> Keys { get; set; } = new();
    }

    internal class JwtKey
    {
        [JsonPropertyName("alg")]
        public string Alg { get; set; } = string.Empty;

        [JsonPropertyName("e")]
        public string E { get; set; } = string.Empty;

        [JsonPropertyName("kid")]
        public string Kid { get; set; } = string.Empty;

        [JsonPropertyName("kty")]
        public string Kty { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public string N { get; set; } = string.Empty;

        [JsonPropertyName("use")]
        public string Use { get; set; } = string.Empty;

        public RSAParameters ToRsaParameters()
        {
            var modulusBase64 = N;
            modulusBase64 = modulusBase64.Replace("_", "/").Replace("-", "+")
                .PadRight(modulusBase64.Length + (4 - modulusBase64.Length % 4) % 4, '=');
            byte[] modulusBytes = Convert.FromBase64String(modulusBase64);

            return new RSAParameters
            {
                Modulus = modulusBytes,
                Exponent = Convert.FromBase64String(E)
            };
        }
    }
}
