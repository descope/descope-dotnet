using Descope.Internal;
using Descope.Internal.Auth;
using Xunit;

namespace Descope.Test.Unit
{
    public class OAuthTests
    {

        [Fact]
        public async Task OAuth_SignUpOrIn()
        {
            var client = new MockHttpClient();
            IOAuth oauth = new OAuth(client);
            client.PostResponse = new { url = "url" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.OAuthSignUpOrIn, url);
                Assert.Equal("github", queryParams!["provider"]);
                Assert.Equal("redirectUrl", queryParams!["redirectUrl"]);
                Assert.Contains("\"stepup\":true", Utils.Serialize(body!));
                Assert.Equal("refreshJwt", pswd);
                return null;
            };
            var response = await oauth.SignUpOrIn(OAuthProvider.Github, redirectUrl: "redirectUrl", loginOptions: new LoginOptions { StepupRefreshJwt = "refreshJwt" });
            Assert.Equal("url", response);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task OAuth_SignUp()
        {
            var client = new MockHttpClient();
            IOAuth oauth = new OAuth(client);
            client.PostResponse = new { url = "url" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.OAuthSignUp, url);
                Assert.Equal("google", queryParams!["provider"]);
                Assert.Equal("redirectUrl", queryParams!["redirectUrl"]);
                Assert.Contains("\"mfa\":true", Utils.Serialize(body!));
                Assert.Equal("refreshJwt", pswd);
                return null;
            };
            var response = await oauth.SignUp(OAuthProvider.Google, redirectUrl: "redirectUrl", loginOptions: new LoginOptions { MfaRefreshJwt = "refreshJwt" });
            Assert.Equal("url", response);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task OAuth_SignIn()
        {
            var client = new MockHttpClient();
            IOAuth oauth = new OAuth(client);
            client.PostResponse = new { url = "url" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.OAuthSignIn, url);
                Assert.Equal("apple", queryParams!["provider"]);
                Assert.Equal("redirectUrl", queryParams!["redirectUrl"]);
                Assert.Null(pswd);
                return null;
            };
            var response = await oauth.SignIn(OAuthProvider.Apple, redirectUrl: "redirectUrl");
            Assert.Equal("url", response);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task OAuth_Update()
        {
            var client = new MockHttpClient();
            IOAuth oauth = new OAuth(client);
            client.PostResponse = new { url = "url" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.OAuthUpdate, url);
                Assert.Equal("test", queryParams!["provider"]);
                Assert.Equal("redirectUrl", queryParams!["redirectUrl"]);
                Assert.Equal("refreshJwt", pswd);
                return null;
            };
            var response = await oauth.UpdateUser("test", "refreshJwt", redirectUrl: "redirectUrl");
            Assert.Equal("url", response);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task OAuth_Exchange()
        {
            var client = new MockHttpClient();
            IOAuth oauth = new OAuth(client);
            client.PostResponse = new AuthenticationResponse("", "", "", "", 0, 0, new UserResponse(new List<string>(), "", ""), false);
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.OAuthExchange, url);
                Assert.Null(queryParams);
                Assert.Contains("\"code\":\"code\"", Utils.Serialize(body!));
                Assert.Null(pswd);
                return null;
            };
            var response = await oauth.Exchange("code");
            Assert.Equal(1, client.PostCount);
        }
    }

}
