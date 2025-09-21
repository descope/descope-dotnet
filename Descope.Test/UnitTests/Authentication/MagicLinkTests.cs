using Descope.Internal;
using Descope.Internal.Auth;
using Xunit;

namespace Descope.Test.Unit
{
    public class MagicLinkTests
    {
        [Fact]
        public async Task MagicLink_Verify_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { sessionJwt = "session_jwt", refreshJwt = "refresh_jwt", user = new { userId = "user123" } };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkVerify, url);
                Assert.Null(pswd);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.Equal("test_token", requestBody.GetProperty("token").GetString());
                return null;
            };

            var response = await magicLink.Verify("test_token");
            
            Assert.Equal("session_jwt", response.SessionJwt);
            Assert.Equal("refresh_jwt", response.RefreshJwt);
            Assert.Equal("user123", response.User.UserId);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_Verify_ThrowsForEmptyToken()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);

            async Task Act() => await magicLink.Verify("");
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Equal("token missing", exception.Message);
            Assert.Equal(0, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_Verify_ThrowsForNullToken()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);

            async Task Act() => await magicLink.Verify(null!);
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Equal("token missing", exception.Message);
            Assert.Equal(0, client.PostCount);
        }
    }
}