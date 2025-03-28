using System.Text.Json;
using Descope.Internal;
using Descope.Internal.Auth;
using Xunit;

namespace Descope.Test.Unit
{
    public class EnchantedLinkTests
    {

        [Fact]
        public async Task EnchantedLink_SignIn()
        {
            var client = new MockHttpClient();
            IEnchantedLink enchanted = new EnchantedLink(client);
            client.PostResponse = new { maskedEmail = "maskedEmail" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.EnchantedLinkSignIn, url);
                Assert.Equal("pswd", pswd);
                EnchantedLinkAuthenticationRequestBody requestBody = Utils.Convert<EnchantedLinkAuthenticationRequestBody>(body);
                Assert.Equal("loginId", requestBody.LoginId);
                Assert.Equal("uri", requestBody.URI);
                Assert.True(requestBody.CrossDevice);
                Assert.True(((JsonElement)requestBody.LoginOptions!["stepup"]!).GetBoolean());
                return null;
            };
            var loginOptions = new LoginOptions { StepupRefreshJwt = "StepupRefreshJwt"};
            var response = await enchanted.SignIn("loginId", "uri", loginOptions, "pswd");
            Assert.Equal("maskedEmail", response.MaskedEmail);
            Assert.Equal(1, client.PostCount);
        }

    }

}
