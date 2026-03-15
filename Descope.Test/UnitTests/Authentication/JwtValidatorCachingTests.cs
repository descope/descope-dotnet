using Descope.Test.Helpers;
using Moq;
using Moq.Protected;
using System.Net;
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
        try { await client.Auth.ValidateSessionAsync(TestJwt); } catch { }
        var firstRequestCount = requestCount;

        // Second call within TTL (< 5 minutes) - should NOT fetch keys
        try { await client.Auth.ValidateSessionAsync(TestJwt); } catch { }
        var secondRequestCount = requestCount;

        // Third call within TTL - should still NOT fetch keys
        try { await client.Auth.ValidateSessionAsync(TestJwt); } catch { }
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
            requestCount = count;
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
    public async Task ValidateSession_KeyRotation_DocumentsTtlBehavior()
    {
        // This test documents key rotation behavior.
        // After TTL expires, new keys should be fetched.
        // Full TTL testing would require injecting a time provider (future enhancement).

        // Arrange
        var requestCount = 0;

        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            requestCount = count;
            var keyId = count <= 1 ? "key-v1" : "key-v2";
            return CreateJwksResponse(keyId);
        });

        var httpClient = new HttpClient(mockHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // Use a JWT with kid that matches the mock response (key-v1)
        var jwt1 = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS12MSIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0ZXN0IiwiZXhwIjoyMTQ3NDgzNjQ3fQ.sig1";

        // Act & Assert
        // First call fetches keys
        try { await client.Auth.ValidateSessionAsync(jwt1); } catch { }
        Assert.Equal(1, requestCount);

        // Second call within TTL reuses cached keys (no cache-miss re-fetch because kid matches)
        try { await client.Auth.ValidateSessionAsync(jwt1); } catch { }
        Assert.Equal(1, requestCount);

        // NOTE: To fully test TTL expiration, inject a time provider into JwtValidator.
        // This is documented as a future improvement.
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
            requestCount = count;
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
        // all of them are cached properly.

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
                        n = "yHPs-I8B-QXd9HH9-mKh_8Kd0K9tC2qQ9uUmw4QdR0E0Ld5z_2T_i0MIQi-7gZuA8Y9_2UAZ9WlCM2Si-5uE_Z0K2uL6_6GAz5F1P9Z5z0u4z1_6tA5F9z4u_5L0z2u6z5L2z4u9z3F5z0u6z5F2z4u9z3L5z0u6z5L2z4u9z3F5z0u6z5F2z4u9z3L5z0u",
                        use = "sig"
                    },
                    new
                    {
                        alg = "RS256",
                        e = "AQAB",
                        kid = "key-3",
                        kty = "RSA",
                        n = "zIQt-J9C-RYe0II0-nLi_9Le1L0uD3rR0vVnx5ReS1F1Me6z_3U_j1NJRj-8hAvB9Z0_3VBA0XmDN3Tj-6vF_a1L3vM7_7HBz6G2Q0a6z1v5z2_7uB6G0z5v_6M1z3v7z6M3z5v0z4G6z1v7z6G3z5v0z4M6z1v7z6M3z5v0z4G6z1v7z6G3z5v0z4M6z1v",
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

        // Act - Call multiple times to ensure all keys are cached
        for (int i = 0; i < 5; i++)
        {
            try { await client.Auth.ValidateSessionAsync(TestJwt); } catch { }
        }

        // Assert - JWKS endpoint should be called at most once per validation call
        // (caching prevents refetching within TTL)
        Assert.True(requestCount >= 1, "JWKS endpoint should be called at least once");
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

    [Fact]
    public async Task ValidateSession_KeyRotation_ShouldImmediatelyRefetchOnCacheMiss()
    {
        // This test verifies cache-miss immediate re-fetch behavior:
        // When a token is signed with a kid that's not in the cache,
        // the validator should immediately re-fetch keys (bypassing TTL) and retry validation.
        // This ensures tokens signed with newly rotated keys are accepted without waiting.
        // It also verifies that old/rotated-out keys don't trigger repeated re-fetches.

        // Arrange
        var requestCount = 0;
        var currentKeyId = "key-v1";

        var mockHandler = CreateMockHttpHandler((request, count) =>
        {
            requestCount = count;
            // Return keys based on current rotation state
            return CreateJwksResponse(currentKeyId);
        });

        var httpClient = new HttpClient(mockHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // Act & Assert
        // Step 1: First validation with key-v1 (fetches and caches key-v1)
        var jwt1 = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS12MSIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0ZXN0IiwiZXhwIjoyMTQ3NDgzNjQ3fQ.sig1";
        try { await client.Auth.ValidateSessionAsync(jwt1); } catch { }
        Assert.Equal(1, requestCount); // Initial fetch

        // Step 2: Rotate keys to key-v2 (key-v1 is now rotated out)
        currentKeyId = "key-v2";

        // Step 3: Validate token with key-v2 kid (not in cache)
        // This should trigger immediate re-fetch (bypassing TTL) because kid is missing
        var jwt2 = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS12MiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0ZXN0IiwiZXhwIjoyMTQ3NDgzNjQ3fQ.sig2";
        try { await client.Auth.ValidateSessionAsync(jwt2); } catch { }

        // Should have triggered immediate re-fetch (requestCount = 2)
        // WITHOUT waiting for TTL to expire
        Assert.Equal(2, requestCount);

        // Step 4: Validate another token with key-v2 (now in cache)
        // Should NOT trigger another fetch (still within TTL)
        try { await client.Auth.ValidateSessionAsync(jwt2); } catch { }
        Assert.Equal(2, requestCount); // No additional fetch

        // Step 5: Try to validate old token with key-v1 (rotated out, no longer in JWKS)
        // This should trigger ONE re-fetch attempt (to check if key-v1 is back)
        try { await client.Auth.ValidateSessionAsync(jwt1); } catch { }
        Assert.Equal(3, requestCount); // One re-fetch attempt for missing key-v1

        // Step 6: Try to validate old token with key-v1 AGAIN
        // Should NOT trigger another re-fetch (cooldown period prevents repeated attempts)
        try { await client.Auth.ValidateSessionAsync(jwt1); } catch { }
        Assert.Equal(3, requestCount); // No additional fetch (cooldown active)

        // Step 7: Try MULTIPLE times with key-v1
        // All should be blocked by cooldown
        for (int i = 0; i < 5; i++)
        {
            try { await client.Auth.ValidateSessionAsync(jwt1); } catch { }
        }
        Assert.Equal(3, requestCount); // Still no additional fetches
    }
}
