using System.Net;
using System.Text;
using Descope;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Descope.Test.UnitTests.Factories;

public class DescopeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDescopeClient_WithoutHttpClientFactoryName_ShouldUseDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key"
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);
        Assert.NotNull(client.Mgmt);
        Assert.NotNull(client.Auth);
    }

    [Fact]
    public void AddDescopeClient_WithCustomHttpClientFactoryName_ShouldUseProvidedName()
    {
        // Arrange
        var services = new ServiceCollection();
        var customName = "MyCustomDescopeClient";
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key",
            HttpClientFactoryName = customName
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);

        // Verify we can also create an HttpClient with the custom name
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
        var httpClient = httpClientFactory.CreateClient(customName);
        Assert.NotNull(httpClient);
    }

    [Fact]
    public void AddDescopeClient_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        DescopeClientOptions options = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddDescopeClient(options));
    }

    [Fact]
    public void AddDescopeClient_WithInvalidOptions_ShouldThrowDescopeException()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "" // Invalid: empty project ID
        };

        // Act & Assert
        Assert.Throws<DescopeException>(() => services.AddDescopeClient(options));
    }

    [Fact]
    public void AddDescopeClient_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key"
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IDescopeClient>());
        Assert.NotNull(serviceProvider.GetService<IHttpClientFactory>());
    }

    [Fact]
    public void AddDescopeClient_WithEmptyHttpClientFactoryName_ShouldUseDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key",
            HttpClientFactoryName = ""
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);

        // Verify the default name "DescopeClient" can be used
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
        var httpClient = httpClientFactory.CreateClient("DescopeClient");
        Assert.NotNull(httpClient);
    }

    [Fact]
    public void AddDescopeClient_WithWhitespaceHttpClientFactoryName_ShouldUseDefaultName()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key",
            HttpClientFactoryName = "   "
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddDescopeClient_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id"
        };

        // Act
        var result = services.AddDescopeClient(options);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddDescopeClient_WithUnsafeOption_ShouldConfigureHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            IsUnsafe = true
        };

        // Act
        services.AddDescopeClient(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IDescopeClient>();
        Assert.NotNull(client);
    }

    // JWT header {"alg":"RS256","kid":"test-key","typ":"JWT"} — kid matches the stub key.
    private const string TestJwt = "eyJhbGciOiJSUzI1NiIsImtpZCI6InRlc3Qta2V5IiwidHlwIjoiSldUIn0.eyJpc3MiOiJ0ZXN0IiwiZXhwIjoyMTQ3NDgzNjQ3fQ.test";

    // descope/etc#16677: JWKS cache must survive DI scopes, so two scopes fetch keys once, not twice.
    [Fact]
    public async Task AddDescopeClient_ValidateSessionAcrossScopes_FetchesKeysOnce()
    {
        // Arrange
        var keysFetchCount = 0;
        var services = new ServiceCollection();
        var options = new DescopeClientOptions
        {
            ProjectId = "test_project_id",
            ManagementKey = "test_management_key",
            BaseUrl = "https://api.descope.com"
        };
        services.AddDescopeClient(options);

        // Last ConfigurePrimaryHttpMessageHandler wins, placing this counting JWKS stub under the SDK pipeline.
        services.AddHttpClient("DescopeClient")
            .ConfigurePrimaryHttpMessageHandler(
                () => new CountingJwksHandler(() => Interlocked.Increment(ref keysFetchCount)));

        var serviceProvider = services.BuildServiceProvider();

        // Act — two separate DI scopes simulate two HTTP requests
        using (var scope1 = serviceProvider.CreateScope())
        {
            var client1 = scope1.ServiceProvider.GetRequiredService<IDescopeClient>();
            try { await client1.Auth.ValidateSessionAsync(TestJwt); } catch (DescopeException) { }
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            var client2 = scope2.ServiceProvider.GetRequiredService<IDescopeClient>();
            try { await client2.Auth.ValidateSessionAsync(TestJwt); } catch (DescopeException) { }
        }

        // Assert — cache shared across scopes: keys endpoint hit once, not once per scope
        Assert.Equal(1, keysFetchCount);
    }

    // Primary handler stub: counts calls to the keys endpoint and returns a static JWKS payload.
    private sealed class CountingJwksHandler : HttpMessageHandler
    {
        // Valid RSA modulus (base64url) so key import in JwtValidator.FetchKeys succeeds.
        private const string KeyModulus = "xGOr-H7A-PWc8GG8-lJg_7Jc9J8sB1pP8tTlv3PcQzD9Kc4z_1S_h9LHPh-6fYtZ7X8_1TZY8VkBL1Rh-4tD_Y9J1tK5_5FZz4E0O8Y4y9t3y0_5sZ4E8z3t_4K9y1t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t5z4K1y3t8z2E4y9t5z4E1y3t8z2K4y9t";

        private readonly Action _onKeysFetch;

        public CountingJwksHandler(Action onKeysFetch) => _onKeysFetch = onKeysFetch;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.Contains("/v2/keys/"))
            {
                _onKeysFetch();
            }

            var json = "{\"keys\":[{\"alg\":\"RS256\",\"e\":\"AQAB\",\"kid\":\"test-key\",\"kty\":\"RSA\",\"use\":\"sig\",\"n\":\""
                + KeyModulus + "\"}]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
