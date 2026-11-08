using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Descope.Test.Helpers;
using Moq;
using Moq.Protected;
using System.Net;
using System.Security.Cryptography;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Tests for JwtValidator JWKS caching behavior, key rotation, and TTL-based refresh.
/// These tests verify fixes for the security vulnerabilities:
/// - CRITICAL: Keys fetched once and never refreshed
/// - HIGH: No concurrent fetch deduplication
/// - HIGH: Unbounded key accumulation
/// </summary>
public class JwtValidatorCachingTests
{
    private const string TestJwt = "eyJhbGciOiJSUzI1NiIsImtpZCI6InRlc3Qta2V5IiwidHlwIjoiSldUIn0.eyJpc3MiOiJ0ZXN0IiwiZXhwIjoyMTQ3NDgzNjQ3fQ.test";

    private static Mock<HttpMessageHandler> CreateMockHttpHandler(
        Func<HttpRequestMessage, int, HttpResponseMessage> responseFactory)
    {
        var requestCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
            {
                return responseFactory(request, Interlocked.Increment(ref requestCount));
            });

        return mockHandler;
    }

    private static HttpResponseMessage CreateJwksResponse(string keyId = "test-key")
    {
        var keysResponse = new
        {
            keys = new[]
            {
                new
                {
                    alg = "RS256",
                    e = "AQAB",
                    kid = keyId,
                    kty = "RSA",
                    n = "xGOr-H7A-PWc8GG8-lJg_7Jc9J8sB1pP8tTlv3PcQzD9Kc4z_1S_h9LHPh-6fYtZ7X8_1TZY8VkBL1Rh-4tD_Y9J1tK5_5FZz4E0O8Y4y9t3y0_5sZ4E8z3t_4K9y1t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t",
                    use = "sig"
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(keysResponse);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task ValidateSession_CalledWithinTtl_ShouldNotRefetchKeys()
    {
        // Arrange
        var requestCount = 0;
        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            requestCount = count;
            return CreateJwksResponse();
        });

        var httpClient = new HttpClient(mockHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // Act
        // First call - should fetch keys
        try { await client.Auth.ValidateSessionAsync(TestJwt); } catch (DescopeException) { }
        var firstRequestCount = requestCount;

        // Second call within TTL (< 5 minutes) - should NOT fetch keys
        try { await client.Auth.ValidateSessionAsync(TestJwt); } catch (DescopeException) { }
        var secondRequestCount = requestCount;

        // Third call within TTL - should still NOT fetch keys
        try { await client.Auth.ValidateSessionAsync(TestJwt); } catch (DescopeException) { }
        var thirdRequestCount = requestCount;

        // Assert
        Assert.Equal(1, firstRequestCount);
        Assert.Equal(1, secondRequestCount); // No additional fetch
        Assert.Equal(1, thirdRequestCount); // Still no additional fetch
    }

    [Fact]
    public async Task ValidateSession_CalledConcurrently_ShouldFetchKeysOnlyOnce()
    {
        // This test verifies that the semaphore-based deduplication works correctly.
        // Multiple concurrent calls should only result in a single JWKS fetch.

        // Arrange
        var requestCount = 0;
        var fetchStartedCount = 0;

        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            Interlocked.Increment(ref fetchStartedCount);
            Interlocked.Exchange(ref requestCount, count);
            return CreateJwksResponse();
        });

        var httpClient = new HttpClient(mockHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // Act - Launch 10 concurrent validation requests
        const int concurrentCalls = 10;
        var tasks = Enumerable.Range(0, concurrentCalls).Select(async _ =>
        {
            try
            {
                await client.Auth.ValidateSessionAsync(TestJwt);
            }
            catch (DescopeException)
            {
                // Expected - invalid JWT signature
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        // Despite 10 concurrent calls, only 1 should have fetched keys
        Assert.Equal(1, requestCount);

        // Verify that concurrent access didn't cause multiple fetch attempts
        Assert.True(fetchStartedCount <= 2,
            $"Expected at most 2 fetch attempts (due to double-check), but got {fetchStartedCount}");
    }

    [Fact]
    public async Task ValidateToken_TtlExpired_ShouldRefetchKeys()
    {
        // Arrange
        var requestCount = 0;
        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            requestCount = count;
            return CreateJwksResponse(); // always returns "test-key", matching TestJwt
        });

        var fakeNow = DateTimeOffset.UtcNow;
        var validator = new JwtValidator(
            "test_project_id",
            "https://api.descope.com",
            new HttpClient(mockHandler.Object),
            timeProvider: () => fakeNow);

        // Act: first validation — fetches keys
        try { await validator.ValidateToken(TestJwt); } catch (DescopeException) { }
        Assert.Equal(1, requestCount);

        // Within TTL — no re-fetch
        try { await validator.ValidateToken(TestJwt); } catch (DescopeException) { }
        Assert.Equal(1, requestCount);

        // Advance time past the 5-minute TTL
        fakeNow = fakeNow.AddMinutes(5).AddSeconds(1);

        // After TTL expiry — should re-fetch
        try { await validator.ValidateToken(TestJwt); } catch (DescopeException) { }
        Assert.Equal(2, requestCount);

        // Next call within new TTL window — no further re-fetch
        try { await validator.ValidateToken(TestJwt); } catch (DescopeException) { }
        Assert.Equal(2, requestCount);
    }

    [Fact]
    public async Task ValidateToken_CustomKeyRefreshInterval_ShouldRespectConfiguredTtl()
    {
        // Arrange
        var requestCount = 0;
        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            requestCount = count;
            return CreateJwksResponse(); // always returns "test-key", matching TestJwt
        });

        var fakeNow = DateTimeOffset.UtcNow;
        var validator = new JwtValidator(
            "test_project_id",
            "https://api.descope.com",
            new HttpClient(mockHandler.Object),
            keyRefreshInterval: TimeSpan.FromMinutes(30),
            timeProvider: () => fakeNow);

        // Act: first validation — fetches keys
        try { await validator.ValidateToken(TestJwt); } catch (DescopeException) { }
        Assert.Equal(1, requestCount);

        // Past the default 5-minute TTL but within the configured 30-minute TTL — no re-fetch
        fakeNow = fakeNow.AddMinutes(10);
        try { await validator.ValidateToken(TestJwt); } catch (DescopeException) { }
        Assert.Equal(1, requestCount);

        // Past the configured 30-minute TTL — should re-fetch
        fakeNow = fakeNow.AddMinutes(21);
        try { await validator.ValidateToken(TestJwt); } catch (DescopeException) { }
        Assert.Equal(2, requestCount);
    }

    [Fact]
    public async Task ValidateSession_ConcurrentCallsDuringKeyRotation_ShouldBeThreadSafe()
    {
        // This test verifies that concurrent calls during key refresh don't cause
        // race conditions, especially when keys are being cleared and repopulated.

        // Arrange
        var requestCount = 0;
        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            Interlocked.Exchange(ref requestCount, count);
            return CreateJwksResponse();
        });

        var httpClient = new HttpClient(mockHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // Act - Launch many concurrent requests
        const int concurrentCalls = 50;
        var tasks = Enumerable.Range(0, concurrentCalls).Select(async _ =>
        {
            try
            {
                await client.Auth.ValidateSessionAsync(TestJwt);
            }
            catch (DescopeException)
            {
                // Expected - invalid JWT
            }
        }).ToArray();

        // Assert - Should complete without race conditions or exceptions
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));
        Assert.Null(exception);

        // Should have fetched keys only once despite concurrent access
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task ValidateSession_MultipleKeysInResponse_ShouldCacheAllKeys()
    {
        // This test verifies that when JWKS endpoint returns multiple keys,
        // all of them are cached and don't trigger cache-miss re-fetches.

        // Arrange
        var requestCount = 0;
        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            requestCount = count;
            var keysResponse = new
            {
                keys = new[]
                {
                    new
                    {
                        alg = "RS256",
                        e = "AQAB",
                        kid = "key-1",
                        kty = "RSA",
                        n = "xGOr-H7A-PWc8GG8-lJg_7Jc9J8sB1pP8tTlv3PcQzD9Kc4z_1S_h9LHPh-6fYtZ7X8_1TZY8VkBL1Rh-4tD_Y9J1tK5_5FZz4E0O8Y4y9t3y0_5sZ4E8z3t_4K9y1t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t",
                        use = "sig"
                    },
                    new
                    {
                        alg = "RS256",
                        e = "AQAB",
                        kid = "key-2",
                        kty = "RSA",
                        n = "xGOr-H7A-PWc8GG8-lJg_7Jc9J8sB1pP8tTlv3PcQzD9Kc4z_1S_h9LHPh-6fYtZ7X8_1TZY8VkBL1Rh-4tD_Y9J1tK5_5FZz4E0O8Y4y9t3y0_5sZ4E8z3t_4K9y1t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t",
                        use = "sig"
                    },
                    new
                    {
                        alg = "RS256",
                        e = "AQAB",
                        kid = "key-3",
                        kty = "RSA",
                        n = "xGOr-H7A-PWc8GG8-lJg_7Jc9J8sB1pP8tTlv3PcQzD9Kc4z_1S_h9LHPh-6fYtZ7X8_1TZY8VkBL1Rh-4tD_Y9J1tK5_5FZz4E0O8Y4y9t3y0_5sZ4E8z3t_4K9y1t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t",
                        use = "sig"
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(keysResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // JWTs with kid matching each key in the response
        var jwtKey1 = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0xIiwidHlwIjoiSldUIn0.eyJpc3MiOiJ0ZXN0IiwiZXhwIjoyMTQ3NDgzNjQ3fQ.sig";
        var jwtKey2 = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0yIiwidHlwIjoiSldUIn0.eyJpc3MiOiJ0ZXN0IiwiZXhwIjoyMTQ3NDgzNjQ3fQ.sig";
        var jwtKey3 = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0zIiwidHlwIjoiSldUIn0.eyJpc3MiOiJ0ZXN0IiwiZXhwIjoyMTQ3NDgzNjQ3fQ.sig";

        // Act — first call fetches all 3 keys
        try { await client.Auth.ValidateSessionAsync(jwtKey1); } catch (DescopeException) { }
        Assert.Equal(1, requestCount);

        // Subsequent calls with different kids should NOT trigger re-fetches
        // because all keys were cached from the single JWKS response
        try { await client.Auth.ValidateSessionAsync(jwtKey2); } catch (DescopeException) { }
        Assert.Equal(1, requestCount);

        try { await client.Auth.ValidateSessionAsync(jwtKey3); } catch (DescopeException) { }
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task ValidateSession_EmptyKeysResponse_ShouldNotCrash()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            var keysResponse = new
            {
                keys = Array.Empty<object>()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(keysResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // Act & Assert - Should throw DescopeException (no keys to validate with)
        // but should NOT crash or throw unexpected exceptions
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Auth.ValidateSessionAsync(TestJwt);
        });
    }

    [Fact]
    public async Task ValidateSession_HttpError_ShouldPropagateException()
    {
        // Arrange
        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Service Unavailable", System.Text.Encoding.UTF8, "text/plain")
            };
        });

        var httpClient = new HttpClient(mockHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // Act & Assert - FetchKeyIfNeeded throws HttpRequestException before try/catch in ValidateToken
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await client.Auth.ValidateSessionAsync(TestJwt);
        });

        Assert.NotNull(exception);
    }

    private sealed class TestSigner
    {
        private readonly RSA _rsa = RSA.Create(2048);

        public TestSigner(string kid) => Kid = kid;

        public string Kid { get; }

        public string JwksJson()
        {
            var p = _rsa.ExportParameters(false);
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                keys = new[]
                {
                    new { alg = "RS256", e = Base64UrlEncoder.Encode(p.Exponent), kid = Kid, kty = "RSA", n = Base64UrlEncoder.Encode(p.Modulus), use = "sig" }
                }
            });
        }

        public string Sign() => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = Kid }, SecurityAlgorithms.RsaSha256)
        });
    }

    /// <summary>A validator wired to a stubbed keys endpoint and a controllable clock.</summary>
    private sealed class Harness
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public int Fetches;
        public HttpStatusCode Status = HttpStatusCode.OK;
        public string Body = "";
        public JwtValidator Validator { get; }

        public Harness(TestSigner served)
        {
            Body = served.JwksJson();
            var handler = CreateMockHttpHandler((_, _) =>
            {
                Interlocked.Increment(ref Fetches);
                return new HttpResponseMessage(Status) { Content = new StringContent(Body) };
            });
            Validator = new JwtValidator("test_project_id", "https://api.descope.com", new HttpClient(handler.Object), timeProvider: () => Now);
        }
    }

    [Fact]
    public async Task ValidateToken_UnknownKid_FetchesAtMostOncePerCooldown()
    {
        var known = new TestSigner("known");
        var unknown = new TestSigner("unknown").Sign();
        var h = new Harness(known);

        await h.Validator.ValidateToken(known.Sign());
        Assert.Equal(1, h.Fetches);

        // Within the cooldown of the initial fetch: rejected without a network call
        await Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(unknown));
        Assert.Equal(1, h.Fetches);

        h.Now = h.Now.AddSeconds(31);
        await Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(unknown));
        Assert.Equal(2, h.Fetches);

        await Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(unknown));
        Assert.Equal(2, h.Fetches);

        h.Now = h.Now.AddSeconds(31);
        await Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(unknown));
        Assert.Equal(3, h.Fetches);
    }

    [Fact]
    public async Task ValidateToken_KeyRotation_AcceptsNewKeyAfterCooldown()
    {
        var v1 = new TestSigner("key-v1");
        var v2 = new TestSigner("key-v2");
        var h = new Harness(v1);

        await h.Validator.ValidateToken(v1.Sign());
        h.Body = v2.JwksJson();
        h.Now = h.Now.AddSeconds(31);

        // Unknown kid triggers a re-fetch inside the TTL, and validation is retried
        await h.Validator.ValidateToken(v2.Sign());
        Assert.Equal(2, h.Fetches);

        await h.Validator.ValidateToken(v2.Sign());
        Assert.Equal(2, h.Fetches);
    }

    [Fact]
    public async Task ValidateToken_TtlExpired_ReplacesCachedKeys()
    {
        var removed = new TestSigner("removed");
        var current = new TestSigner("current");
        var h = new Harness(removed);

        await h.Validator.ValidateToken(removed.Sign());
        h.Body = current.JwksJson();

        // Within the TTL the cached key is still trusted
        h.Now = h.Now.AddMinutes(1);
        await h.Validator.ValidateToken(removed.Sign());
        Assert.Equal(1, h.Fetches);

        // After the TTL the set is replaced, not merged
        h.Now = h.Now.AddMinutes(5);
        await Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(removed.Sign()));
        await h.Validator.ValidateToken(current.Sign());
        Assert.Equal(2, h.Fetches);
    }

    [Fact]
    public async Task ValidateToken_FailedRefresh_KeepsCachedKeysAndLimitsRetries()
    {
        var signer = new TestSigner("key");
        var h = new Harness(signer);

        await h.Validator.ValidateToken(signer.Sign());
        h.Status = HttpStatusCode.ServiceUnavailable;
        h.Now = h.Now.AddMinutes(6);

        // Refresh fails, the last good keys keep working
        await h.Validator.ValidateToken(signer.Sign());
        Assert.Equal(2, h.Fetches);

        // Retry is limited by the cooldown even though the keys are still past the TTL
        await h.Validator.ValidateToken(signer.Sign());
        Assert.Equal(2, h.Fetches);

        h.Now = h.Now.AddSeconds(31);
        h.Status = HttpStatusCode.OK;
        await h.Validator.ValidateToken(signer.Sign());
        Assert.Equal(3, h.Fetches);

        // Success restores the TTL window
        await h.Validator.ValidateToken(signer.Sign());
        Assert.Equal(3, h.Fetches);
    }

    [Fact]
    public async Task ValidateToken_EndpointDown_StopsUsingCachedKeysAfterOneHour()
    {
        var signer = new TestSigner("key");
        var h = new Harness(signer);

        await h.Validator.ValidateToken(signer.Sign());
        var lastSuccess = h.Now;
        h.Status = HttpStatusCode.ServiceUnavailable;

        // Cached key keeps validating while refreshes fail, up to the 1 hour bound
        h.Now = lastSuccess.AddMinutes(59).AddSeconds(59);
        await h.Validator.ValidateToken(signer.Sign());

        // Past the bound the cached set is not used: rejected while the failed-refresh
        // throttle skips the fetch, and the fetch error surfaces once it retries
        h.Now = lastSuccess.AddHours(1);
        await Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(signer.Sign()));
        Assert.Equal(2, h.Fetches);
        h.Now = h.Now.AddSeconds(31);
        await Assert.ThrowsAsync<HttpRequestException>(() => h.Validator.ValidateToken(signer.Sign()));
        Assert.Equal(3, h.Fetches);

        // Recovery restores validation
        h.Status = HttpStatusCode.OK;
        h.Now = h.Now.AddSeconds(31);
        await h.Validator.ValidateToken(signer.Sign());
    }

    [Fact]
    public async Task ValidateToken_CacheDurationAboveOneHour_RefreshesAfterOneHour()
    {
        var signer = new TestSigner("key");
        var fetches = 0;
        var now = DateTimeOffset.UtcNow;
        var handler = CreateMockHttpHandler((_, _) =>
        {
            Interlocked.Increment(ref fetches);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(signer.JwksJson()) };
        });
        var validator = new JwtValidator("test_project_id", "https://api.descope.com", new HttpClient(handler.Object), TimeSpan.FromHours(2), () => now);

        await validator.ValidateToken(signer.Sign());
        now = now.AddHours(1);
        await validator.ValidateToken(signer.Sign());
        Assert.Equal(2, fetches);
    }

    [Fact]
    public async Task ValidateToken_FailedFetch_DoesNotDelayUnknownKid()
    {
        var v1 = new TestSigner("key-v1");
        var v2 = new TestSigner("key-v2");
        var h = new Harness(v1) { Status = HttpStatusCode.ServiceUnavailable };

        // First fetch fails; the next lookup retries right away and succeeds
        await Assert.ThrowsAsync<HttpRequestException>(() => h.Validator.ValidateToken(v1.Sign()));
        h.Status = HttpStatusCode.OK;
        await h.Validator.ValidateToken(v1.Sign());
        Assert.Equal(2, h.Fetches);

        // Refresh past the TTL fails; a newly rotated kid is still fetched without waiting
        h.Now = h.Now.AddMinutes(6);
        h.Status = HttpStatusCode.ServiceUnavailable;
        await h.Validator.ValidateToken(v1.Sign());
        Assert.Equal(3, h.Fetches);
        h.Status = HttpStatusCode.OK;
        h.Body = v2.JwksJson();
        await h.Validator.ValidateToken(v2.Sign());
        Assert.Equal(4, h.Fetches);
    }

    [Fact]
    public async Task ValidateToken_EmptyKeysResponse_ReplacesCachedKeys()
    {
        var signer = new TestSigner("key");
        var h = new Harness(signer);

        await h.Validator.ValidateToken(signer.Sign());
        h.Body = "{\"keys\":[]}";
        h.Now = h.Now.AddMinutes(6);

        await Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(signer.Sign()));
        Assert.Equal(2, h.Fetches);

        // The empty set counts as a successful fetch: no refetch inside the TTL or cooldown
        await Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(signer.Sign()));
        Assert.Equal(2, h.Fetches);
    }

    [Fact]
    public async Task ValidateToken_ConcurrentUnknownKids_ShareOneFetch()
    {
        var known = new TestSigner("known");
        var unknown = new TestSigner("unknown").Sign();
        var h = new Harness(known);

        await h.Validator.ValidateToken(known.Sign());
        h.Now = h.Now.AddSeconds(31);

        var tasks = Enumerable.Range(0, 20).Select(_ =>
            Task.Run(() => Assert.ThrowsAsync<DescopeException>(() => h.Validator.ValidateToken(unknown))));
        await Task.WhenAll(tasks);

        Assert.Equal(2, h.Fetches);
    }
}
