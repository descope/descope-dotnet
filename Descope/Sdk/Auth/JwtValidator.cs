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
/// </summary>
internal class JwtValidator
{
    private readonly JsonWebTokenHandler _jsonWebTokenHandler = new();
    private ConcurrentDictionary<string, List<SecurityKey>> _securityKeys = new();
    private readonly string _projectId;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private readonly TimeSpan _keyRefreshInterval;
    private long _lastKeyFetchTicks = 0;
    private long _lastKeyFetchAttemptTicks = 0;
    private bool _lastKeyFetchFailed = false;
    private static readonly TimeSpan KeyRefetchCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxKeyAge = TimeSpan.FromHours(1);
    private readonly Func<DateTimeOffset> _timeProvider;

    public JwtValidator(string projectId, string baseUrl, HttpClient httpClient, TimeSpan? keyRefreshInterval = null, Func<DateTimeOffset>? timeProvider = null)
    {
        _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _keyRefreshInterval = keyRefreshInterval ?? TimeSpan.FromMinutes(5);
        _timeProvider = timeProvider ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Validates a JWT token locally using cached public keys.
    /// Implements cache-miss re-fetch: if validation fails because the key ID (kid)
    /// from the token is not in the cache, re-fetches the keys and retries validation once.
    /// Such re-fetches happen at most once per 30 seconds (measured from the last successful fetch),
    /// so tokens signed with newly rotated keys are accepted quickly while unknown key IDs
    /// cannot trigger a fetch on every request.
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

            // Extract the kid (key ID) from the token header
            var kid = token.Kid;

            var result = await _jsonWebTokenHandler.ValidateTokenAsync(jwt, BuildValidationParameters());

            // Cache-miss re-fetch: if validation failed and the kid is NOT in the cache,
            // re-fetch keys (bypassing TTL, limited by the cooldown) and retry validation once.
            // This handles key rotation scenarios where a token is signed with a newly rotated key.
            if (!result.IsValid && kid != null && !_securityKeys.ContainsKey(kid))
            {
                // Kid not in cache - this might be a newly rotated key
                await ForceKeyFetch();

                // Retry validation once with the newly fetched keys
                result = await _jsonWebTokenHandler.ValidateTokenAsync(jwt, BuildValidationParameters());
            }

            if (result.Exception != null) throw new DescopeException("JWT validation failed");
            return result.IsValid ? new Token(token) : throw new DescopeException("JWT validation failed");
        }
        catch (Exception ex)
        {
            throw new DescopeException("JWT validation failed", ex);
        }
    }

    private TokenValidationParameters BuildValidationParameters() => new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
        {
            if (kid != null && KeysWithinMaxAge() && _securityKeys.TryGetValue(kid, out var keys))
            {
                return keys;
            }
            return new List<SecurityKey>();
        },
        ValidateIssuerSigningKey = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ClockSkew = TimeSpan.FromSeconds(5),
    };

    private TimeSpan KeyAge() =>
        TimeSpan.FromTicks(_timeProvider().Ticks - Interlocked.Read(ref _lastKeyFetchTicks));

    private bool KeysAreFresh() => KeyAge() < _keyRefreshInterval && KeysWithinMaxAge();

    // Cached keys are not used once the last successful fetch is older than MaxKeyAge
    private bool KeysWithinMaxAge() => KeyAge() < MaxKeyAge;

    private async Task FetchKeyIfNeeded()
    {
        if (!KeysAreFresh())
        {
            await FetchKeys(force: false);
        }
    }

    private async Task ForceKeyFetch() => await FetchKeys(force: true);

    private async Task FetchKeys(bool force)
    {
        // Use semaphore to ensure only one thread fetches at a time
        await _fetchSemaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock - another thread might have fetched.
            // Unknown kid (force): fetch at most once per cooldown since the last successful fetch;
            // a failed fetch does not delay the next unknown kid lookup.
            // Stale keys: refresh, unless a refresh attempt failed within the cooldown, so cached
            // keys keep validating without waiting on a failing endpoint.
            var now = _timeProvider().Ticks;
            var skip = force
                ? TimeSpan.FromTicks(now - Interlocked.Read(ref _lastKeyFetchTicks)) < KeyRefetchCooldown
                : KeysAreFresh() || (_lastKeyFetchFailed && TimeSpan.FromTicks(now - _lastKeyFetchAttemptTicks) < KeyRefetchCooldown);
            if (skip)
            {
                return;
            }

            _lastKeyFetchAttemptTicks = now;
            _lastKeyFetchFailed = true;

            var url = $"{_baseUrl.TrimEnd('/')}/v2/keys/{_projectId}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var keyResponse = JsonSerializer.Deserialize<JwtKeyResponse>(content);

            if (keyResponse?.Keys == null) return;

            // Build new key map atomically to avoid racing with in-flight validations
            var newKeys = new ConcurrentDictionary<string, List<SecurityKey>>();

            foreach (var key in keyResponse.Keys)
            {
                var rsa = RSA.Create();
                rsa.ImportParameters(key.ToRsaParameters());

                newKeys.AddOrUpdate(
                    key.Kid,
                    _ => new List<SecurityKey> { new RsaSecurityKey(rsa) },
                    (_, existingKeys) =>
                    {
                        return existingKeys.Concat(new[] { new RsaSecurityKey(rsa) }).ToList();
                    });
            }

            // Atomically swap in the new keys — in-flight validations continue
            // using the old dictionary reference until they complete
            Interlocked.Exchange(ref _securityKeys, newKeys);

            // Update last fetch time AFTER successful fetch
            Interlocked.Exchange(ref _lastKeyFetchTicks, _timeProvider().Ticks);
            _lastKeyFetchFailed = false;
        }
        catch when (Interlocked.Read(ref _lastKeyFetchTicks) != 0 && KeysWithinMaxAge())
        {
            // Failed refresh: keep the last good key set and its fetch time. The attempt time
            // was recorded above, so refreshes of stale cached keys are limited by the cooldown.
            // With no usable key set (none fetched yet, or older than MaxKeyAge) the error propagates.
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

    }
