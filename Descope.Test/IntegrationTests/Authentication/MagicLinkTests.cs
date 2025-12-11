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
using System.Text.Json;
using Xunit;
using Descope.Auth.Models.Onetimev1;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    public class MagicLinkScenario
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
    }

    [Collection("Integration Tests")]
    public class MagicLinkTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task MagicLink_Verify_Success()
        {
            string? loginId = null;
            try
            {
                // Create a test user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "Magic Link Test User"
                };

                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Generate magic link for test user with custom claims
                var customClaims = new Descope.Mgmt.Models.Onetimev1.LoginOptions_customClaims
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "testKey", "testValue" },
                        { "numericKey", 42 }
                    }
                };

                var loginOptions = new Descope.Mgmt.Models.Onetimev1.LoginOptions
                {
                    CustomClaims = customClaims
                };

                var magicLinkRequest = new Descope.Mgmt.Models.Onetimev1.TestUserGenerateMagicLinkRequest
                {
                    DeliveryMethod = "email",
                    LoginId = testLoginId,
                    RedirectUrl = "https://example.com/auth",
                    LoginOptions = loginOptions
                };

                var magicLinkResponse = await _descopeClient.Mgmt.V1.Tests.Generate.Magiclink.PostAsync(magicLinkRequest);

                // Extract token from the magic link URL
                var uri = new Uri(magicLinkResponse!.Link!);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var token = queryParams["t"];

                Assert.NotNull(token);
                Assert.NotEmpty(token);

                // Verify the magic link token
                var verifyRequest = new VerifyMagicLinkRequest
                {
                    Token = token
                };
                var authResponse = await _descopeClient.Auth.V1.Magiclink.Verify.PostAsync(verifyRequest);

                // Verify the response
                Assert.NotNull(authResponse);
                Assert.NotEmpty(authResponse.SessionJwt!);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt);
                Assert.Equal(user!.User!.UserId, authResponse.User?.UserId);
                Assert.Equal(testLoginId, authResponse.User?.Email);

                // Validate the session JWT
                var validatedToken = await _descopeClient.Auth.ValidateSessionAsync(authResponse.SessionJwt!);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);

                // Verify custom claims are present in the token
                // Custom claims added via LoginOptions are nested under 'nsec' claim according to documentation
                // Let's check if nsec claim exists, if not check the claims directly
                if (validatedToken.Claims.ContainsKey("nsec"))
                {
                    var nsecClaim = validatedToken.Claims["nsec"] as Dictionary<string, object>;
                    Assert.NotNull(nsecClaim);
                    Assert.Contains("testKey", nsecClaim.Keys);
                    Assert.Equal("testValue", nsecClaim["testKey"].ToString());
                    Assert.Contains("numericKey", nsecClaim.Keys);
                    Assert.Equal("42", nsecClaim["numericKey"].ToString());
                }
                else
                {
                    // If not in nsec, check directly in claims (for test users or different flow)
                    Assert.Contains("testKey", validatedToken.Claims.Keys);
                    Assert.Equal("testValue", validatedToken.Claims["testKey"].ToString());
                    Assert.Contains("numericKey", validatedToken.Claims.Keys);
                    Assert.Equal("42", validatedToken.Claims["numericKey"].ToString());
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task MagicLink_Verify_WithInvalidToken_ShouldFail()
        {
            // Try to verify an invalid magic link token
            var verifyRequest = new VerifyMagicLinkRequest { Token = "invalid_token_123" };
            async Task Act() => await _descopeClient.Auth.V1.Magiclink.Verify.PostAsync(verifyRequest);

            // Should throw an exception for invalid token
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("token", exception.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task MagicLink_Verify_WithEmptyToken_ShouldFail()
        {
            // Try to verify an empty magic link token
            var verifyRequest = new VerifyMagicLinkRequest { Token = "" };
            async Task Act() => await _descopeClient.Auth.V1.Magiclink.Verify.PostAsync(verifyRequest);

            // Should throw an exception for empty token
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("token", exception.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task MagicLink_Verify_WithNullToken_ShouldFail()
        {
            // Try to verify a null magic link token
            var verifyRequest = new VerifyMagicLinkRequest { Token = null };
            async Task Act() => await _descopeClient.Auth.V1.Magiclink.Verify.PostAsync(verifyRequest);

            // Should throw an exception for null token
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("token", exception.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task MagicLink_GenerateEmbeddedLink_WithCustomClaims_Success()
        {
            string? loginId = null;
            try
            {
                // Create a user with email
                var testLoginId = Guid.NewGuid().ToString() + "@descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "Embedded Link User"
                };
                var user = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Generate embedded link for test user with custom claims
                var customClaims = new Descope.Mgmt.Models.Managementv1.EmbeddedLinkSignInRequest_customClaims
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "embeddedTestKey", "embeddedTestValue" },
                        { "numericKey", 123 },
                        { "booleanKey", true }
                    }
                };

                var embeddedLinkRequest = new Descope.Mgmt.Models.Managementv1.EmbeddedLinkSignInRequest
                {
                    LoginId = testLoginId,
                    CustomClaims = customClaims
                };

                var embeddedLinkResponse = await _descopeClient.Mgmt.V1.User.Signin.Embeddedlink.PostAsync(embeddedLinkRequest);
                var token = embeddedLinkResponse!.Token;

                Assert.NotNull(token);
                Assert.NotEmpty(token);

                // Verify the embedded link token using magic link verification
                var verifyRequest = new VerifyMagicLinkRequest { Token = token };
                var authResponse = await _descopeClient.Auth.V1.Magiclink.Verify.PostAsync(verifyRequest);

                // Verify the response
                Assert.NotNull(authResponse);
                Assert.NotEmpty(authResponse.SessionJwt!);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt);
                Assert.Equal(user!.User!.UserId, authResponse.User?.UserId);
                Assert.Equal(testLoginId, authResponse.User?.Email);

                // Validate the session JWT
                var validatedToken = await _descopeClient.Auth.ValidateSessionAsync(authResponse.SessionJwt!);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);

                // Verify custom claims are present in the token
                // Custom claims should be directly in the claims or nested under 'nsec'
                if (validatedToken.Claims.ContainsKey("nsec"))
                {
                    var nsecClaim = validatedToken.Claims["nsec"] as Dictionary<string, object>;
                    Assert.NotNull(nsecClaim);
                    Assert.Contains("embeddedTestKey", nsecClaim.Keys);
                    Assert.Equal("embeddedTestValue", nsecClaim["embeddedTestKey"].ToString());
                    Assert.Contains("numericKey", nsecClaim.Keys);
                    Assert.Equal("123", nsecClaim["numericKey"].ToString());
                    Assert.Contains("booleanKey", nsecClaim.Keys);
                    Assert.Equal("true", nsecClaim["booleanKey"].ToString());
                }
                else
                {
                    // If not in nsec, check directly in claims
                    Assert.Contains("embeddedTestKey", validatedToken.Claims.Keys);
                    Assert.Equal("embeddedTestValue", validatedToken.Claims["embeddedTestKey"].ToString());
                    Assert.Contains("numericKey", validatedToken.Claims.Keys);
                    Assert.Equal("123", validatedToken.Claims["numericKey"].ToString());
                    Assert.Contains("booleanKey", validatedToken.Claims.Keys);
                    Assert.Equal("true", validatedToken.Claims["booleanKey"].ToString());
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task MagicLink_GenerateEmbeddedLink_WithTimeout_Expired()
        {
            string? loginId = null;
            try
            {
                // Create a user with email
                var testLoginId = Guid.NewGuid().ToString() + "@descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "Embedded Link Timeout Test User"
                };
                var user = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Generate embedded link with a 1 second timeout
                var embeddedLinkRequest = new Descope.Mgmt.Models.Managementv1.EmbeddedLinkSignInRequest
                {
                    LoginId = testLoginId,
                    Timeout = 1
                };

                var embeddedLinkResponse = await _descopeClient.Mgmt.V1.User.Signin.Embeddedlink.PostAsync(embeddedLinkRequest);
                var token = embeddedLinkResponse!.Token;

                Assert.NotNull(token);
                Assert.NotEmpty(token);

                // Sleep for 2 seconds to ensure the token expires
                await Task.Delay(2000);

                // Try to verify the expired token - should fail
                var verifyRequest = new VerifyMagicLinkRequest { Token = token };
                async Task Act() => await _descopeClient.Auth.V1.Magiclink.Verify.PostAsync(verifyRequest);

                // Should throw an exception for expired token
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("expire", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }
    }
}
