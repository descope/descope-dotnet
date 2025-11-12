/*
MagicLinkTests - Integration Test Flow

Each test uses a scenario from AppSettings.MagicLink.Scenarios:
{
  "name": "signin|signup|sign-in-or-up",
  "email": "...",
  "redirectUrl": "..." // If empty, test expects user to paste received link into config after email is sent
}

Behavior:
1. If redirectUrl is empty:
    - Cleanup user at the beginning
    - Send the email
    - Prompt user to paste the received link into appsettingsTest.json (do NOT cleanup user at end)
2. If redirectUrl is set:
    - Do NOT send email
    - Validate the URL from config
    - Cleanup user at the end

Example appsettingsTest.json object:
{
  "AppSettings": {
    "ProjectId": "P35NSSBAL90fyZmyOdrqAc76ZGst",
    "ManagementKey": "***********",
    "BaseURL": "http://localhost:8000",
    "Unsafe": "false",
    "MagicLink": {
      "Scenarios": [
        {
          "name": "signin",
          "email": "yosi+1@descope.com",
          "redirectUrl": ""
        },
        {
          "name": "signup",
          "email": "yosi+2@descope.com",
          "redirectUrl": ""
        },
        {
          "name": "sign-in-or-up",
          "email": "yosi+3@descope.com",
          "redirectUrl": ""
        }
      ]
    }
  }
}

*/
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Descope.Test.Integration
{
    public class MagicLinkScenario
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
    }

    public class MagicLinkTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();
        private readonly IConfiguration _configuration;

        public MagicLinkTests()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettingsTest.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static string GetTokenFromUrl(string url)
        {
            var uri = new Uri(url);
            // verify that the part until the query is https://example.com/auth
            if (uri.GetLeftPart(UriPartial.Path) != "https://example.com/auth")
                throw new InvalidOperationException("Magic link URL does not match expected redirect URL");
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var token = queryParams["t"];
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Magic link token not found in the provided URL");
            return token;
        }

        private MagicLinkScenario GetScenario(string name)
        {
            var scenarios = _configuration.GetSection("AppSettings:MagicLink:Scenarios").Get<List<MagicLinkScenario>>();
            var scenario = scenarios?.FirstOrDefault(s => s.Name == name);
            if (scenario == null)
                throw new ApplicationException($"Scenario '{name}' not found in appsettingsTest.json");
            return scenario;
        }

        [Fact]
        public async Task MagicLink_Verify_Success()
        {
            string? loginId = null;
            try
            {
                // Create a test user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var user = await _descopeClient.Management.User.Create(testLoginId, new UserRequest()
                {
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "Magic Link Test User"
                }, testUser: true);
                loginId = testLoginId;

                // Generate magic link for test user
                var magicLinkResponse = await _descopeClient.Management.User.GenerateMagicLinkForTestUser(
                    DeliveryMethod.Email,
                    testLoginId,
                    "https://example.com/auth"
                );

                // Extract token from the magic link URL
                var uri = new Uri(magicLinkResponse.Link);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var token = queryParams["t"];

                Assert.NotNull(token);
                Assert.NotEmpty(token);

                // Verify the magic link token
                var authResponse = await _descopeClient.Auth.MagicLink.Verify(token);

                // Verify the response
                Assert.NotNull(authResponse);
                Assert.NotEmpty(authResponse.SessionJwt);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt);
                Assert.Equal(user.UserId, authResponse.User.UserId);
                Assert.Equal(testLoginId, authResponse.User.Email);

                // Validate the session JWT
                var validatedToken = await _descopeClient.Auth.ValidateSession(authResponse.SessionJwt);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task MagicLink_SignIn_Email_Success()
        {
            var scenario = GetScenario("signin");
            var testLoginId = scenario.Email;
            var redirectUrl = scenario.RedirectUrl;

            if (string.IsNullOrEmpty(redirectUrl))
            {
                // Cleanup user at start
                try { await _descopeClient.Management.User.Delete(testLoginId); } catch { }
                // Create user directly (not via sign-up magic link)
                var user = await _descopeClient.Management.User.Create(testLoginId, new UserRequest
                {
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = $"Magic Link SignIn Test User {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                });
                // Sanity check - user should exist now
                var loadedUser = await _descopeClient.Management.User.Load(user.UserId);
                Assert.Equal(user.UserId, loadedUser.UserId);
                // Send sign-in magic link
                await _descopeClient.Auth.MagicLink.SignIn(DeliveryMethod.Email, testLoginId, "https://example.com/auth");
                throw new InvalidOperationException("CHECK YOUR EMAIL! Waiting for user to paste magic link into config.");
            }
            else
            {
                // Validate the URL
                var token = GetTokenFromUrl(redirectUrl);
                var authResponse = await _descopeClient.Auth.MagicLink.Verify(token);
                Assert.NotNull(authResponse);
                Assert.NotNull(authResponse.SessionJwt);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.User.UserId);
                // Verify the userId in the token matches the original user
                var loadedUser = await _descopeClient.Management.User.Load(authResponse.User.UserId);
                Assert.Equal(testLoginId, loadedUser.Email);

                // Validate the session JWT
                var validatedToken = await _descopeClient.Auth.ValidateSession(authResponse.SessionJwt);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);

                // Cleanup user at end
                await _descopeClient.Management.User.Delete(testLoginId);
            }
        }

        [Fact]
        public async Task MagicLink_SignUp_Email_Success()
        {
            var scenario = GetScenario("signup");
            var testLoginId = scenario.Email;
            var redirectUrl = scenario.RedirectUrl;

            if (string.IsNullOrEmpty(redirectUrl))
            {
                // Cleanup user at start
                try { await _descopeClient.Management.User.Delete(testLoginId); } catch { }
                // Send sign-up magic link
                await _descopeClient.Auth.MagicLink.SignUp(DeliveryMethod.Email, testLoginId, "https://example.com/auth", new SignUpDetails
                {
                    Email = testLoginId,
                    Name = $"Magic Link SignUp Test User {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
                });
                throw new InvalidOperationException("CHECK YOUR EMAIL! Waiting for user to paste magic link into config.");
            }
            else
            {
                // Validate the URL
                var token = GetTokenFromUrl(redirectUrl);
                var authResponse = await _descopeClient.Auth.MagicLink.Verify(token);
                Assert.NotNull(authResponse);
                Assert.NotNull(authResponse.SessionJwt);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.User.UserId);
                // Verify the userId in the token matches the original user
                var loadedUser = await _descopeClient.Management.User.Load(authResponse.User.UserId);
                Assert.Equal(testLoginId, loadedUser.Email);
                // Verify the name matches the sign-up details
                Assert.Equal(loadedUser.Name, authResponse.User.Name);

                // Validate the session JWT
                var validatedToken = await _descopeClient.Auth.ValidateSession(authResponse.SessionJwt);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);

                // Cleanup user at end
                await _descopeClient.Management.User.Delete(testLoginId);
            }
        }

        [Fact]
        public async Task MagicLink_SignUpOrIn_Email_Success()
        {
            var scenario = GetScenario("sign-in-or-up");
            var testLoginId = scenario.Email;
            var redirectUrl = scenario.RedirectUrl;

            if (string.IsNullOrEmpty(redirectUrl))
            {
                // Cleanup user at start
                try { await _descopeClient.Management.User.Delete(testLoginId); } catch { }
                // Send sign-up-or-in magic link
                await _descopeClient.Auth.MagicLink.SignUpOrIn(DeliveryMethod.Email, testLoginId, "https://example.com/auth", new SignUpOptions
                {
                    CustomClaims = new Dictionary<string, object>
                    {
                        { "name", $"Magic Link SignUpOrIn Test User {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" }
                    }
                });
                throw new InvalidOperationException("CHECK YOUR EMAIL! Waiting for user to paste magic link into config.");
            }
            else
            {
                // Validate the URL
                var token = GetTokenFromUrl(redirectUrl);
                var authResponse = await _descopeClient.Auth.MagicLink.Verify(token);
                Assert.NotNull(authResponse);
                Assert.NotNull(authResponse.SessionJwt);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.User.UserId);
                // Verify the userId in the token matches the original user
                var loadedUser = await _descopeClient.Management.User.Load(authResponse.User.UserId);
                Assert.Equal(testLoginId, loadedUser.Email);

                // Validate the session JWT
                var validatedToken = await _descopeClient.Auth.ValidateSession(authResponse.SessionJwt);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);

                // Cleanup user at end
                await _descopeClient.Management.User.Delete(testLoginId);
            }
        }

        [Fact]
        public async Task MagicLink_Verify_WithInvalidToken_ShouldFail()
        {
            // Try to verify an invalid magic link token
            async Task Act() => await _descopeClient.Auth.MagicLink.Verify("invalid_token_123");

            // Should throw an exception for invalid token
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("token", exception.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task MagicLink_Verify_WithEmptyToken_ShouldFail()
        {
            // Try to verify an empty magic link token
            async Task Act() => await _descopeClient.Auth.MagicLink.Verify("");

            // Should throw an exception for empty token
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Equal("token missing", exception.Message);
        }

        [Fact]
        public async Task MagicLink_Verify_WithNullToken_ShouldFail()
        {
            // Try to verify a null magic link token
            async Task Act() => await _descopeClient.Auth.MagicLink.Verify(null!);

            // Should throw an exception for null token
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Equal("token missing", exception.Message);
        }
    }
}
