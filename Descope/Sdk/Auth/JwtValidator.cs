using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Descope;

/// <summary>
/// Handles JWT validation using locally cached public keys.
/// This class is thread-safe and can be used concurrently from multiple threads.
/// Keys are cached with a TTL and automatically refreshed to support key rotation.
/// </summary>
internal class JwtValidator : IDisposable
{
    private readonly JsonWebTokenHandler _jsonWebTokenHandler = new();
    private readonly ConcurrentDictionary<string, List<SecurityKey>> _securityKeys = new();
    private readonly string _projectId;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private readonly TimeSpan _keysCacheTtl;
    private DateTime? _lastFetchTime;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the JwtValidator class.
    /// </summary>
    /// <param name="projectId">The Descope project ID.</param>
    /// <param name="baseUrl">The base URL for the Descope API.</param>
    /// <param name="httpClient">An HttpClient instance for fetching JWKS.</param>
    /// <param name="keysCacheTtl">Optional TTL for cached keys. Defaults to 10 minutes.</param>
    public JwtValidator(string projectId, string baseUrl, HttpClient httpClient, TimeSpan? keysCacheTtl = null)
    {
        _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _keysCacheTtl = keysCacheTtl ?? TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Validates a JWT token locally using cached public keys.
    /// </summary>
    /// <param name="jwt">The JWT token to validate.</param>
    /// <returns>A validated Token object or null if validation fails.</returns>
    public async Task<Token> ValidateToken(string jwt)
    {
        if (string.IsNullOrEmpty(jwt)) throw new DescopeException("JWT cannot be empty");

        await FetchKeyIfNeeded();

        try
        {
            var token = _jsonWebTokenHandler.ReadJsonWebToken(jwt);
            if (token == null) throw new DescopeException("Failed to read JWT token");

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

            if (result.Exception != null) throw new DescopeException("JWT validation failed");
            return result.IsValid ? new Token(token) : throw new DescopeException("JWT validation failed");
        }
        catch (Exception ex)
        {
            throw new DescopeException("JWT validation failed", ex);
        }
    }

    private async Task FetchKeyIfNeeded()
    {
        // Check if cache is still valid based on TTL
        var now = DateTime.UtcNow;
        if (_lastFetchTime.HasValue && (now - _lastFetchTime.Value) < _keysCacheTtl)
        {
            return; // Cache is still fresh
        }

        // Use semaphore to ensure only one thread fetches at a time (deduplication)
        await _fetchSemaphore.WaitAsync();
        try
        {
            // Double-check after acquiring semaphore (another thread might have just fetched)
            now = DateTime.UtcNow;
            if (_lastFetchTime.HasValue && (now - _lastFetchTime.Value) < _keysCacheTtl)
            {
                return;
            }

            var url = $"{_baseUrl.TrimEnd('/')}/v2/keys/{_projectId}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var keyResponse = JsonSerializer.Deserialize<JwtKeyResponse>(content);

            if (keyResponse?.Keys == null) return;

            // Clear old keys to prevent unbounded accumulation (replace, don't accumulate)
            _securityKeys.Clear();

            foreach (var key in keyResponse.Keys)
            {
                var rsa = RSA.Create();
                rsa.ImportParameters(key.ToRsaParameters());

                _securityKeys.AddOrUpdate(
                    key.Kid,
                    _ => new List<SecurityKey> { new RsaSecurityKey(rsa) },
                    (_, existingKeys) =>
                    {
                        return existingKeys.Concat(new[] { new RsaSecurityKey(rsa) }).ToList();
                    });
            }

            // Update last fetch time AFTER successful fetch
            _lastFetchTime = DateTime.UtcNow;
        }
        finally
        {
            _fetchSemaphore.Release();
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

    /// <summary>
    /// Disposes resources used by the JwtValidator.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _fetchSemaphore?.Dispose();
        _disposed = true;
    }
}
