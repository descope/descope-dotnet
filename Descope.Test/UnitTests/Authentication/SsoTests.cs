using Descope.Internal;
using Descope.Internal.Auth;
using Xunit;

namespace Descope.Test.Unit
{
    public class SsoTests
    {

        [Fact]
        public async Task SSO_Start()
        {
            var client = new MockHttpClient();
            ISsoAuth sso = new Sso(client);
            client.PostResponse = new { url = "url" };
            client.PostAssert = (url, body, queryParams) =>
            {
                Assert.Equal(Routes.SsoStart, url);
                Assert.Equal("tenant", queryParams!["tenant"]);
                Assert.Equal("redirectUrl", queryParams!["redirectUrl"]);
                Assert.Equal("prompt", queryParams!["prompt"]);
                Assert.Contains("\"stepup\":true", Utils.Serialize(body!));
                return null;
            };
            var response = await sso.Start("tenant", redirectUrl: "redirectUrl", prompt: "prompt", loginOptions: new LoginOptions { StepUp = true });
            Assert.Equal("url", response);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task SSO_Exchange()
        {
            var client = new MockHttpClient();
            ISsoAuth sso = new Sso(client);
            client.PostResponse = new AuthenticationResponse("", "", "", "", 0, 0, new UserResponse(new List<string>(), "", ""), false);
            client.PostAssert = (url, body, queryParams) =>
            {
                Assert.Equal(Routes.SsoExchange, url);
                Assert.Null(queryParams);
                Assert.Contains("\"code\":\"code\"", Utils.Serialize(body!));
                return null;
            };
            var response = await sso.Exchange("code");
            Assert.Equal(1, client.PostCount);
        }
    }
}
