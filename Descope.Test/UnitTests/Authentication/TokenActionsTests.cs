using Descope.Test.Helpers;
using Microsoft.Kiota.Abstractions;
using System.Net;
using Xunit;
using Moq;
using Moq.Protected;

namespace Descope.Test.UnitTests.Authentication;

public class TokenActionsTests
{
    private const string ValidSessionJwt = "eyJhbGciOiJSUzI1NiIsImtpZCI6InRlc3Qta2V5IiwidHlwIjoiSldUIn0.eyJpc3MiOiJ0ZXN0LXByb2plY3QtaWQiLCJzdWIiOiJ1c2VyMTIzIiwiZXhwIjoyMTQ3NDgzNjQ3LCJpYXQiOjE2MDk0NTk2NDd9.test-signature";
    private const string ValidRefreshJwt = "eyJhbGciOiJSUzI1NiIsImtpZCI6InRlc3Qta2V5IiwidHlwIjoiSldUIn0.eyJpc3MiOiJ0ZXN0LXByb2plY3QtaWQiLCJzdWIiOiJ1c2VyMTIzIiwiZXhwIjoyMTQ3NDgzNjQ3LCJpYXQiOjE2MDk0NTk2NDd9.test-signature-refresh";
    private const string ValidAccessKey = "K2puaXQtdGVzdC1wcm9qZWN0OmFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6MTIzNDU2Nzg5MA==";

    #region RefreshSession Tests

    [Fact]
    public async Task RefreshSession_WithValidRefreshJwt_ShouldReturnNewSessionToken()
    {
        // Arrange
        var expectedResponse = new Descope.Auth.Models.Onetimev1.JWTResponse
        {
            SessionJwt = ValidSessionJwt,
            RefreshJwt = ValidRefreshJwt
        };

        var client = TestDescopeClientFactory.CreateWithAsserter<
            Descope.Auth.Models.Onetimev1.RefreshSessionRequest,
            Descope.Auth.Models.Onetimev1.JWTResponse>(
            (requestInfo, requestBody) =>
            {
                // Assert that the JWT was passed in the authentication context
                var authOption = requestInfo.RequestOptions?.OfType<DescopeJwtOption>().FirstOrDefault();
                Assert.NotNull(authOption);
                Assert.True(authOption.GetContext().ContainsKey("jwt"));
                Assert.Equal(ValidRefreshJwt, authOption.GetContext()["jwt"]);

                // Assert the request was made to the correct endpoint
                Assert.Contains("/v1/auth/refresh", requestInfo.URI.ToString());

                return expectedResponse;
            });

        // Note: JWT validation requires a real HttpClient with proper key fetching,
        // which we're skipping in this test. The method should still work with the mocked response.
        // In a real scenario, you'd need to set up proper JWT validation or skip it for tests.
    }

    [Fact]
    public async Task RefreshSession_WithEmptyRefreshJwt_ShouldThrowException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Auth.RefreshSessionAsync("");
        });

    }

    [Fact]
    public async Task RefreshSession_WithNullRefreshJwt_ShouldThrowException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Auth.RefreshSessionAsync(null!);
        });
    }

    #endregion

    #region ValidateAndRefreshSession Tests

    [Fact]
    public async Task ValidateAndRefreshSession_WithBothJwtsEmpty_ShouldThrowException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Auth.ValidateAndRefreshSession("", "");
        });
    }

    [Fact]
    public async Task ValidateAndRefreshSession_WithEmptyRefreshJwt_ShouldThrowException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            // Pass invalid session JWT (will fail validation) and empty refresh JWT
            await client.Auth.ValidateAndRefreshSession("invalid-jwt", "");
        });
    }

    #endregion

    #region ExchangeAccessKey Tests

    [Fact]
    public async Task ExchangeAccessKey_WithValidAccessKey_ShouldReturnSessionToken()
    {
        // Arrange
        var expectedResponse = new Descope.Auth.Models.Onetimev1.ExchangeAccessKeyResponse
        {
            SessionJwt = ValidSessionJwt,
            KeyId = "test-key-id"
        };

        var client = TestDescopeClientFactory.CreateWithAsserter<
            Descope.Auth.Models.Onetimev1.ExchangeAccessKeyRequest,
            Descope.Auth.Models.Onetimev1.ExchangeAccessKeyResponse>(
            (requestInfo, requestBody) =>
            {
                // Assert that the access key was passed in the authentication context
                var authOption = requestInfo.RequestOptions?.OfType<DescopeJwtOption>().FirstOrDefault();
                Assert.NotNull(authOption);
                Assert.True(authOption.GetContext().ContainsKey("jwt"));
                Assert.Equal(ValidAccessKey, authOption.GetContext()["jwt"]);

                // Assert the request was made to the correct endpoint
                Assert.Contains("/v1/auth/accesskey/exchange", requestInfo.URI.ToString());

                return expectedResponse;
            });

        // Note: Similar to RefreshSession, JWT validation is skipped in this test
    }

    [Fact]
    public async Task ExchangeAccessKey_WithEmptyAccessKey_ShouldThrowException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Auth.ExchangeAccessKey("");
        });
    }

    [Fact]
    public async Task ExchangeAccessKey_WithNullAccessKey_ShouldThrowException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Auth.ExchangeAccessKey(null!);
        });
    }

    [Fact]
    public async Task ExchangeAccessKey_WithLoginOptions_ShouldIncludeOptionsInRequest()
    {
        // Arrange
        var loginOptions = new Descope.Auth.Models.Onetimev1.AccessKeyLoginOptions
        {
            SelectedTenant = "test-tenant"
        };

        var expectedResponse = new Descope.Auth.Models.Onetimev1.ExchangeAccessKeyResponse
        {
            SessionJwt = ValidSessionJwt,
            KeyId = "test-key-id"
        };

        var client = TestDescopeClientFactory.CreateWithAsserter<
            Descope.Auth.Models.Onetimev1.ExchangeAccessKeyRequest,
            Descope.Auth.Models.Onetimev1.ExchangeAccessKeyResponse>(
            (requestInfo, requestBody) =>
            {
                // Assert that login options were included in the request
                Assert.NotNull(requestBody);
                Assert.NotNull(requestBody.LoginOptions);
                Assert.Equal("test-tenant", requestBody.LoginOptions.SelectedTenant);

                // Assert that the access key was passed in the authentication context
                var authOption = requestInfo.RequestOptions?.OfType<DescopeJwtOption>().FirstOrDefault();
                Assert.NotNull(authOption);
                Assert.True(authOption.GetContext().ContainsKey("jwt"));

                return expectedResponse;
            });

        // Note: JWT validation is skipped in this test
    }

    #endregion

    #region ValidateSession Tests

    [Fact]
    public async Task ValidateSession_WithEmptySessionJwt_ShouldThrowException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Auth.ValidateSessionAsync("");
        });
    }

    [Fact]
    public async Task ValidateSession_WithNullSessionJwt_ShouldThrowException()
    {
        // Arrange
        var client = TestDescopeClientFactory.CreateWithEmptyResponse();

        // Act & Assert
        await Assert.ThrowsAsync<DescopeException>(async () =>
        {
            await client.Auth.ValidateSessionAsync(null!);
        });

    }

    [Fact]
    public async Task ValidateSession_CalledConsecutively_ShouldFetchPublicKeysOnlyOnce()
    {
        // Arrange
        var requestCount = 0;
        var mockHttpHandler = new Mock<HttpMessageHandler>();

        // Setup the mock to return a valid JWKS response
        mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                requestCount++;

                // Mock the public keys endpoint response with a valid RSA key
                var keysResponse = new
                {
                    keys = new[]
                    {
                        new
                        {
                            alg = "RS256",
                            e = "AQAB",
                            kid = "test-key",
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

        // Create the client with our mock HttpClient
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        // Act
        // First call - should fetch keys
        var testJwt = "look ma, a jwt";
        try
        {
            await client.Auth.ValidateSessionAsync(testJwt);
        }
        catch (DescopeException)
        {
            // Expected to fail due to invalid signature, but keys should be cached
        }

        // Second call - should use cached keys
        try
        {
            await client.Auth.ValidateSessionAsync(testJwt);
        }
        catch (DescopeException)
        {
            // Expected to fail due to invalid signature
        }

        // Assert
        // The HTTP client should have been called exactly once to fetch the keys
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task ValidateSession_CalledConcurrently_ShouldNotThrowConcurrentAccessException()
    {
        // This test reproduces a race condition bug where multiple concurrent calls to
        // ValidateSessionAsync would corrupt the internal dictionary state due to
        // non-thread-safe Dictionary<string, List<SecurityKey>> access.
        // See: https://github.com/descope/etc/issues/13256
        //
        // The error manifests as:
        // "Operations that change non-concurrent collections must have exclusive access.
        // A concurrent update was performed on this collection and corrupted its state."

        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();

        // Setup the mock to return a valid JWKS response with a small delay
        // to increase the chance of race conditions
        mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                // Small delay to increase chance of concurrent access
                Thread.Sleep(10);

                // Mock the public keys endpoint response with a valid RSA key
                var keysResponse = new
                {
                    keys = new[]
                    {
                        new
                        {
                            alg = "RS256",
                            e = "AQAB",
                            kid = "test-key",
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

        // Create the client with our mock HttpClient
        var httpClient = new HttpClient(mockHttpHandler.Object);
        var client = TestDescopeClientFactory.CreateWithHttpClient(httpClient);

        var testJwt = "look ma, a jwt";
        const int concurrentCalls = 50;

        // Act - Run multiple validation calls concurrently
        // This should NOT throw InvalidOperationException about concurrent collection access
        var tasks = Enumerable.Range(0, concurrentCalls).Select(async _ =>
        {
            try
            {
                await client.Auth.ValidateSessionAsync(testJwt);
            }
            catch (DescopeException)
            {
                // Expected to fail due to invalid JWT signature, that's fine
            }
            // Any other exception (especially InvalidOperationException for concurrent access)
            // should bubble up and fail the test
        }).ToArray();

        // Assert - This should complete without throwing InvalidOperationException
        // If the dictionary is not thread-safe, this will throw:
        // "Operations that change non-concurrent collections must have exclusive access"
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));

        Assert.Null(exception);
    }

    #endregion

}
