using System.Net;
using System.Text;
using System.Text.Json;
using Descope.Internal;
using Xunit;

namespace Descope.Test.UnitTests.Internal;

public class CookieToBodyHandlerTests
{
    private const string SampleSessionJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.session.signature";
    private const string SampleRefreshJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.refresh.signature";

    [Fact]
    public async Task SendAsync_WithCookiesAndEmptyBodyFields_PatchesJwtsIntoBody()
    {
        // Arrange - body has empty sessionJwt/refreshJwt, cookies have the real values
        var bodyJson = JsonSerializer.Serialize(new { sessionJwt = "", refreshJwt = "", user = new { userId = "u1" } });
        var handler = CreateHandler(bodyJson, new Dictionary<string, string>
        {
            [CookieToBodyHandler.SessionCookieName] = SampleSessionJwt,
            [CookieToBodyHandler.RefreshCookieName] = SampleRefreshJwt
        });

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert - JWTs patched from cookies, other fields preserved
        Assert.Equal(SampleSessionJwt, doc.RootElement.GetProperty("sessionJwt").GetString());
        Assert.Equal(SampleRefreshJwt, doc.RootElement.GetProperty("refreshJwt").GetString());
        Assert.Equal("u1", doc.RootElement.GetProperty("user").GetProperty("userId").GetString());
    }

    [Fact]
    public async Task SendAsync_WithCookiesAndMissingBodyFields_PatchesJwtsIntoBody()
    {
        // Arrange - body doesn't have sessionJwt/refreshJwt at all
        var bodyJson = JsonSerializer.Serialize(new { user = new { userId = "u1" } });
        var handler = CreateHandler(bodyJson, new Dictionary<string, string>
        {
            [CookieToBodyHandler.SessionCookieName] = SampleSessionJwt,
            [CookieToBodyHandler.RefreshCookieName] = SampleRefreshJwt
        });

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert - JWTs added from cookies
        Assert.Equal(SampleSessionJwt, doc.RootElement.GetProperty("sessionJwt").GetString());
        Assert.Equal(SampleRefreshJwt, doc.RootElement.GetProperty("refreshJwt").GetString());
        Assert.Equal("u1", doc.RootElement.GetProperty("user").GetProperty("userId").GetString());
    }

    [Fact]
    public async Task SendAsync_WithCookiesButBodyAlreadyHasJwts_DoesNotOverwrite()
    {
        // Arrange - body already has valid JWTs
        var existingSession = "existing.session.jwt";
        var existingRefresh = "existing.refresh.jwt";
        var bodyJson = JsonSerializer.Serialize(new { sessionJwt = existingSession, refreshJwt = existingRefresh });
        var handler = CreateHandler(bodyJson, new Dictionary<string, string>
        {
            [CookieToBodyHandler.SessionCookieName] = SampleSessionJwt,
            [CookieToBodyHandler.RefreshCookieName] = SampleRefreshJwt
        });

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert - existing values preserved, NOT overwritten by cookies
        Assert.Equal(existingSession, doc.RootElement.GetProperty("sessionJwt").GetString());
        Assert.Equal(existingRefresh, doc.RootElement.GetProperty("refreshJwt").GetString());
    }

    [Fact]
    public async Task SendAsync_WithNullBodyJwts_PatchesFromCookies()
    {
        // Arrange - body has null sessionJwt/refreshJwt
        var bodyJson = "{\"sessionJwt\":null,\"refreshJwt\":null,\"user\":{\"userId\":\"u1\"}}";
        var handler = CreateHandler(bodyJson, new Dictionary<string, string>
        {
            [CookieToBodyHandler.SessionCookieName] = SampleSessionJwt,
            [CookieToBodyHandler.RefreshCookieName] = SampleRefreshJwt
        });

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert - null values replaced with cookie values
        Assert.Equal(SampleSessionJwt, doc.RootElement.GetProperty("sessionJwt").GetString());
        Assert.Equal(SampleRefreshJwt, doc.RootElement.GetProperty("refreshJwt").GetString());
    }

    [Fact]
    public async Task SendAsync_WithOnlySessionCookie_PatchesOnlySessionJwt()
    {
        // Arrange - only DS cookie, no DSR
        var bodyJson = JsonSerializer.Serialize(new { sessionJwt = "", refreshJwt = "" });
        var handler = CreateHandler(bodyJson, new Dictionary<string, string>
        {
            [CookieToBodyHandler.SessionCookieName] = SampleSessionJwt
        });

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert - only sessionJwt patched
        Assert.Equal(SampleSessionJwt, doc.RootElement.GetProperty("sessionJwt").GetString());
        Assert.Equal("", doc.RootElement.GetProperty("refreshJwt").GetString());
    }

    [Fact]
    public async Task SendAsync_WithOnlyRefreshCookie_PatchesOnlyRefreshJwt()
    {
        // Arrange - only DSR cookie, no DS
        var bodyJson = JsonSerializer.Serialize(new { sessionJwt = "", refreshJwt = "" });
        var handler = CreateHandler(bodyJson, new Dictionary<string, string>
        {
            [CookieToBodyHandler.RefreshCookieName] = SampleRefreshJwt
        });

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert - only refreshJwt patched
        Assert.Equal("", doc.RootElement.GetProperty("sessionJwt").GetString());
        Assert.Equal(SampleRefreshJwt, doc.RootElement.GetProperty("refreshJwt").GetString());
    }

    [Fact]
    public async Task SendAsync_WithoutCookies_DoesNotModifyBody()
    {
        // Arrange - no Set-Cookie headers
        var bodyJson = JsonSerializer.Serialize(new { sessionJwt = "", refreshJwt = "" });
        var handler = CreateHandler(bodyJson);

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert - body unchanged
        Assert.Equal("", doc.RootElement.GetProperty("sessionJwt").GetString());
        Assert.Equal("", doc.RootElement.GetProperty("refreshJwt").GetString());
    }

    [Fact]
    public async Task SendAsync_WithUnrelatedCookies_DoesNotModifyBody()
    {
        // Arrange - Set-Cookie headers with non-Descope cookie names
        var bodyJson = JsonSerializer.Serialize(new { sessionJwt = "", refreshJwt = "" });
        var handler = CreateHandler(bodyJson, new Dictionary<string, string>
        {
            ["PHPSESSID"] = "abc123",
            ["TrackingId"] = "xyz789"
        });

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert - body unchanged
        Assert.Equal("", doc.RootElement.GetProperty("sessionJwt").GetString());
        Assert.Equal("", doc.RootElement.GetProperty("refreshJwt").GetString());
    }

    [Fact]
    public async Task SendAsync_WithErrorResponse_DoesNotModifyBody()
    {
        // Arrange - non-success response should be left alone
        var bodyJson = JsonSerializer.Serialize(new { errorCode = "E111119", errorDescription = "Unauthorized" });
        var handler = CreateHandler(bodyJson, new Dictionary<string, string>
        {
            [CookieToBodyHandler.SessionCookieName] = SampleSessionJwt,
            [CookieToBodyHandler.RefreshCookieName] = SampleRefreshJwt
        }, HttpStatusCode.Unauthorized);

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - body unchanged for error responses
        Assert.Equal(bodyJson, content);
    }

    [Fact]
    public async Task SendAsync_WithNonJsonBody_DoesNotThrow()
    {
        // Arrange - non-JSON response body
        var handler = CreateHandler("not json at all", new Dictionary<string, string>
        {
            [CookieToBodyHandler.SessionCookieName] = SampleSessionJwt
        });

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.descope.com/v1/auth/otp/verify/email");

        // Act - should not throw
        var response = await invoker.SendAsync(request, CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - original content preserved
        Assert.Equal("not json at all", content);
    }

    #region Test Helpers

    private static CookieToBodyHandler CreateHandler(
        string bodyJson,
        Dictionary<string, string>? cookies = null,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new CookieToBodyHandler
        {
            InnerHandler = new MockResponseHandler(bodyJson, cookies, statusCode)
        };
    }

    /// <summary>
    /// Returns a fixed response with optional Set-Cookie headers.
    /// </summary>
    private class MockResponseHandler : HttpMessageHandler
    {
        private readonly string _bodyJson;
        private readonly Dictionary<string, string>? _cookies;
        private readonly HttpStatusCode _statusCode;

        public MockResponseHandler(
            string bodyJson,
            Dictionary<string, string>? cookies = null,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _bodyJson = bodyJson;
            _cookies = cookies;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_bodyJson, Encoding.UTF8, "application/json")
            };

            if (_cookies != null)
            {
                foreach (var cookie in _cookies)
                {
                    response.Headers.TryAddWithoutValidation("Set-Cookie",
                        $"{cookie.Key}={cookie.Value}; Path=/; HttpOnly; Secure");
                }
            }

            return Task.FromResult(response);
        }
    }

    #endregion
}
