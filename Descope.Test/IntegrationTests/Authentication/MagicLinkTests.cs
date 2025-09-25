using Xunit;

namespace Descope.Test.Integration
{
    public class MagicLinkTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        public MagicLinkTests()
        {
            // Clean up test users to avoid hitting the limit
            try
            {
                _descopeClient.Management.User.DeleteAllTestUsers().GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore errors during cleanup
            }
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