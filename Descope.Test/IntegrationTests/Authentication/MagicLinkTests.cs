using Xunit;

namespace Descope.Test.Integration
{
    public class MagicLinkTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

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