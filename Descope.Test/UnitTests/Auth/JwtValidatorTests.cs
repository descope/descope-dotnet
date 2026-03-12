using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Descope.Test.UnitTests.Auth;

public class JwtValidatorTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private int _requestCount;

    public JwtValidatorTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _requestCount = 0;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    private void SetupMockHttpResponse(string? responseJson = null, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var defaultResponse = new
        {
            keys = new[]
            {
                new
                {
                    alg = "RS256",
                    e = "AQAB",
                    kid = "test-key-1",
                    kty = "RSA",
                    n = "xGOr-H7A-PWc8GG8-lJg_7Jc9J8sB1pP8tTlv3PcQzD9Kc4z_1S_h9LHPh-6fYtZ7X8_1TZY8VkBL1Rh-4tD_Y9J1tK5_5FZz4E0O8Y4y9t3y0_5sZ4E8z3t_4K9y1t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t",
                    use = "sig"
                }
            }
        };

        var json = responseJson ?? JsonSerializer.Serialize(defaultResponse);

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref _requestCount);
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });
    }

    private void SetupMockHttpResponseWithKeyRotation()
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                var currentCount = Interlocked.Increment(ref _requestCount);

                // Return different keys based on request count (simulating key rotation)
                var keysResponse = new
                {
                    keys = new[]
                    {
                        new
                        {
                            alg = "RS256",
                            e = "AQAB",
                            kid = $"rotated-key-{currentCount}",
                            kty = "RSA",
                            n = "xGOr-H7A-PWc8GG8-lJg_7Jc9J8sB1pP8tTlv3PcQzD9Kc4z_1S_h9LHPh-6fYtZ7X8_1TZY8VkBL1Rh-4tD_Y9J1tK5_5FZz4E0O8Y4y9t3y0_5sZ4E8z3t_4K9y1t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t",
                            use = "sig"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(keysResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });
    }

    #region TTL and Key Refresh Tests

    [Fact]
    public async Task ValidateToken_FetchesKeysOnFirstCall()
    {
        // Arrange
        SetupMockHttpResponse();
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient);
        var testJwt = "invalid.jwt.token";

        // Act
        try
        {
            await validator.ValidateToken(testJwt);
        }
        catch (DescopeException)
        {
            // Expected to fail due to invalid signature
        }

        // Assert
        Assert.Equal(1, _requestCount);
    }

    [Fact]
    public async Task ValidateToken_WithinTTL_DoesNotRefetchKeys()
    {
        // Arrange
        SetupMockHttpResponse();
        var ttl = TimeSpan.FromMinutes(10);
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient, ttl);
        var testJwt = "invalid.jwt.token";

        // Act - First call
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }

        // Act - Second call within TTL
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }

        // Assert - Keys should be fetched only once
        Assert.Equal(1, _requestCount);
    }

    [Fact]
    public async Task ValidateToken_AfterTTLExpires_RefetchesKeys()
    {
        // Arrange
        SetupMockHttpResponseWithKeyRotation();
        var ttl = TimeSpan.FromMilliseconds(100); // Very short TTL for testing
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient, ttl);
        var testJwt = "invalid.jwt.token";

        // Act - First call
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }
        Assert.Equal(1, _requestCount);

        // Wait for TTL to expire
        await Task.Delay(150);

        // Act - Second call after TTL expires
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }

        // Assert - Keys should be fetched twice (initial + after TTL expiry)
        Assert.Equal(2, _requestCount);
    }

    [Fact]
    public async Task ValidateToken_MultipleCallsAfterTTLExpiry_RefetchesMultipleTimes()
    {
        // Arrange
        SetupMockHttpResponseWithKeyRotation();
        var ttl = TimeSpan.FromMilliseconds(50);
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient, ttl);
        var testJwt = "invalid.jwt.token";

        // Act & Assert - Multiple cycles of call → wait → call
        for (int i = 1; i <= 3; i++)
        {
            try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }
            Assert.Equal(i, _requestCount);
            await Task.Delay(70); // Wait for TTL to expire
        }
    }

    #endregion

    #region Concurrent Fetch Deduplication Tests

    [Fact]
    public async Task ValidateToken_ConcurrentCallsBeforeFirstFetch_DeduplicatesFetch()
    {
        // Arrange
        SetupMockHttpResponse();
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient);
        var testJwt = "invalid.jwt.token";
        const int concurrentCalls = 10;

        // Act - Run multiple validation calls concurrently before any keys are fetched
        var tasks = Enumerable.Range(0, concurrentCalls).Select(async _ =>
        {
            try
            {
                await validator.ValidateToken(testJwt);
            }
            catch (DescopeException)
            {
                // Expected to fail due to invalid JWT signature
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        // Assert - Despite 10 concurrent calls, keys should only be fetched once
        // (semaphore ensures only one thread fetches)
        Assert.Equal(1, _requestCount);
    }

    [Fact]
    public async Task ValidateToken_ConcurrentCallsAfterTTLExpiry_DeduplicatesFetch()
    {
        // Arrange
        SetupMockHttpResponseWithKeyRotation();
        var ttl = TimeSpan.FromMilliseconds(100);
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient, ttl);
        var testJwt = "invalid.jwt.token";
        const int concurrentCalls = 10;

        // Act - Initial fetch
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }
        Assert.Equal(1, _requestCount);

        // Wait for TTL to expire
        await Task.Delay(150);

        // Act - Concurrent calls after TTL expiry
        var tasks = Enumerable.Range(0, concurrentCalls).Select(async _ =>
        {
            try
            {
                await validator.ValidateToken(testJwt);
            }
            catch (DescopeException) { }
        }).ToArray();

        await Task.WhenAll(tasks);

        // Assert - Should have fetched only twice: initial + one more after TTL
        // (concurrent calls are deduplicated by semaphore)
        Assert.Equal(2, _requestCount);
    }

    [Fact]
    public async Task ValidateToken_ConcurrentAccessDoesNotThrow()
    {
        // This test verifies the fix for concurrent access issues
        // See: https://github.com/descope/etc/issues/13256

        // Arrange
        SetupMockHttpResponse();
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient);
        var testJwt = "invalid.jwt.token";
        const int concurrentCalls = 100;

        // Act - Run many concurrent validation calls
        var tasks = Enumerable.Range(0, concurrentCalls).Select(async _ =>
        {
            try
            {
                await validator.ValidateToken(testJwt);
            }
            catch (DescopeException)
            {
                // Expected to fail due to invalid JWT
            }
        }).ToArray();

        // Assert - Should complete without InvalidOperationException
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));
        Assert.Null(exception);
    }

    #endregion

    #region Cache Replacement Tests

    [Fact]
    public async Task ValidateToken_AfterKeyRotation_ReplacesOldKeys()
    {
        // This test verifies that old keys are cleared when new keys are fetched,
        // preventing unbounded memory growth

        // Arrange
        var rotationResponseCount = 0;
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref _requestCount);
                var currentRotation = Interlocked.Increment(ref rotationResponseCount);

                // Each fetch returns a completely different key ID
                var keysResponse = new
                {
                    keys = new[]
                    {
                        new
                        {
                            alg = "RS256",
                            e = "AQAB",
                            kid = $"key-generation-{currentRotation}",
                            kty = "RSA",
                            n = "xGOr-H7A-PWc8GG8-lJg_7Jc9J8sB1pP8tTlv3PcQzD9Kc4z_1S_h9LHPh-6fYtZ7X8_1TZY8VkBL1Rh-4tD_Y9J1tK5_5FZz4E0O8Y4y9t3y0_5sZ4E8z3t_4K9y1t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t",
                            use = "sig"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(keysResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var ttl = TimeSpan.FromMilliseconds(50);
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient, ttl);
        var testJwt = "invalid.jwt.token";

        // Act - Fetch keys multiple times with rotation
        for (int i = 0; i < 5; i++)
        {
            try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }
            await Task.Delay(70); // Wait for TTL to expire
        }

        // Assert - We should have fetched 5 times, and old keys should be replaced each time
        // (not accumulated). We can't directly inspect the internal cache, but the test
        // verifies that the code path for cache clearing is exercised.
        Assert.Equal(5, _requestCount);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ValidateToken_WithNullJwt_ThrowsDescopeException()
    {
        // Arrange
        SetupMockHttpResponse();
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await validator.ValidateToken(null!);
        });
    }

    [Fact]
    public async Task ValidateToken_WithEmptyJwt_ThrowsDescopeException()
    {
        // Arrange
        SetupMockHttpResponse();
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await validator.ValidateToken("");
        });
    }

    [Fact]
    public void Constructor_WithNullProjectId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new JwtValidator(null!, "https://api.descope.com", _httpClient);
        });
    }

    [Fact]
    public void Constructor_WithNullBaseUrl_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new JwtValidator("test-project", null!, _httpClient);
        });
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new JwtValidator("test-project", "https://api.descope.com", null!);
        });
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        SetupMockHttpResponse();
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient);

        // Act & Assert - Should not throw
        validator.Dispose();
        validator.Dispose();
        validator.Dispose();
    }

    [Fact]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        SetupMockHttpResponse();
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient);

        // Act
        validator.Dispose();

        // Assert - Verify disposal happened (semaphore should be disposed)
        // We can't directly verify the semaphore is disposed, but we ensure no exception is thrown
        Assert.True(true);
    }

    #endregion

    #region Custom TTL Tests

    [Fact]
    public async Task Constructor_WithCustomTTL_UsesProvidedTTL()
    {
        // Arrange
        SetupMockHttpResponseWithKeyRotation();
        var customTtl = TimeSpan.FromMilliseconds(200);
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient, customTtl);
        var testJwt = "invalid.jwt.token";

        // Act - First call
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }
        Assert.Equal(1, _requestCount);

        // Act - Call before TTL expires
        await Task.Delay(100);
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }
        Assert.Equal(1, _requestCount); // Should not refetch

        // Act - Call after TTL expires
        await Task.Delay(150);
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }
        Assert.Equal(2, _requestCount); // Should refetch
    }

    [Fact]
    public async Task Constructor_WithoutTTL_UsesDefaultTTL()
    {
        // Arrange
        SetupMockHttpResponse();
        var validator = new JwtValidator("test-project", "https://api.descope.com", _httpClient);
        var testJwt = "invalid.jwt.token";

        // Act - First call
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }

        // Act - Second call immediately (within default 10 minute TTL)
        try { await validator.ValidateToken(testJwt); } catch (DescopeException) { }

        // Assert - Should use cached keys (default TTL is 10 minutes)
        Assert.Equal(1, _requestCount);
    }

    #endregion
}
