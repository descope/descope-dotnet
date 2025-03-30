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
                EnchantedLink.SignInRequest requestBody = Utils.Convert<EnchantedLink.SignInRequest>(body);
                Assert.Equal("loginId", requestBody.LoginId);
                Assert.Equal("uri", requestBody.URI);
                Assert.True(requestBody.CrossDevice);
                Assert.True(((JsonElement)requestBody.LoginOptions!["stepup"]!).GetBoolean());
                return null;
            };
            var loginOptions = new LoginOptions { StepupRefreshJwt = "StepupRefreshJwt" };
            var response = await enchanted.SignIn("loginId", "uri", loginOptions, "pswd");
            Assert.Equal("maskedEmail", response.MaskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task EnchantedLink_SignUp()
        {
            var client = new MockHttpClient();
            IEnchantedLink enchanted = new EnchantedLink(client);
            client.PostResponse = new { maskedEmail = "maskedEmail" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.EnchantedLinkSignUp, url);
                Assert.Null(pswd);
                EnchantedLink.SignUpRequest requestBody = Utils.Convert<EnchantedLink.SignUpRequest>(body);
                Assert.Equal("loginId", requestBody.LoginId);
                Assert.Equal("uri", requestBody.URI);
                Assert.Equal("email", requestBody.User.Email);
                Assert.Equal("smith", requestBody.User.FamilyName);
                Assert.Equal("test", requestBody.LoginOptions.TemplateID);
                Assert.True(requestBody.LoginOptions.TemplateOptions!.ContainsKey("key"));
                return null;
            };
            var signUpDetails = new SignUpDetails { Email = "email", FamilyName = "smith" };
            var signUpOptions = new SignUpOptions { TemplateID = "test", TemplateOptions = new Dictionary<string, string> { { "key", "value" } } };
            var response = await enchanted.SignUp("loginId", "uri", signUpDetails, signUpOptions);
            Assert.Equal("maskedEmail", response.MaskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task EnchantedLink_SignUpOrIn()
        {
            var client = new MockHttpClient();
            IEnchantedLink enchanted = new EnchantedLink(client);
            client.PostResponse = new { maskedEmail = "maskedEmail" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.EnchantedLinkSignUpOrIn, url);
                Assert.Null(pswd);
                EnchantedLink.SignUpOrInRequest requestBody = Utils.Convert<EnchantedLink.SignUpOrInRequest>(body);
                Assert.Equal("loginId", requestBody.LoginId);
                Assert.Equal("uri", requestBody.URI);
                Assert.Equal("test", requestBody.LoginOptions.TemplateID);
                Assert.True(requestBody.LoginOptions.TemplateOptions!.ContainsKey("key"));
                return null;
            };
            var signUpOptions = new SignUpOptions { TemplateID = "test", TemplateOptions = new Dictionary<string, string> { { "key", "value" } } };
            var response = await enchanted.SignUpOrIn("loginId", "uri", signUpOptions);
            Assert.Equal("maskedEmail", response.MaskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task EnchantedLink_GetSession()
        {
            var client = new MockHttpClient();
            IEnchantedLink enchanted = new EnchantedLink(client);
            // Mock the response for GetSession with a captured valid response
            var sessionJwt = "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMnVsUTZlYndJeks1QWc5cUdnWDdna2tHb0toIiwidHlwIjoiSldUIn0.eyJhbXIiOlsiZW1haWwiXSwiZHJuIjoiRFMiLCJleHAiOjE3NDMzMzkyMDksImlhdCI6MTc0MzMzODYwOSwiaXNzIjoiUDJ1bFE2VktyUXg3ZHFsNGR2elp2Q01lYkdTUCIsInJleHAiOiIyMDI1LTA0LTI3VDEyOjQzOjI5WiIsInN1YiI6IlUydjJNRkF4VHhscWJZUWFrNndxN1dZSUdFZGkifQ.YYtOCF2Od5UMlz1xt4IJd5mFOlV6m7h04qI96novNNm2hqEIMduDtFh5YjBRYmx3cEb5ajdtnTmAnwMj7Jf8N96FsQmMtwgzW_evimKGchqEJBfyAsOiIgvF2DZVsIW9c3opCMuNU6YHWhqmUjpOPWj5USwO4KYoZ804GjdkTx93wjzE411qIrFecEXxjwBmNBEyH4tXl-MOxUgsFvnSdQHGdBVyrAhKSFk_dabnwkjEGVPm0Q-0bWikLiCqhG009-1Mzl4SXL-XZArD_Iwq641D2FfXRqmgH157Pryyvl0ms8RFPBL1uNW7ZcFaxjX1fYLpG6KlVnsNAnoiyYZyPg";
            var refreshJwt = "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMnVsUTZlYndJeks1QWc5cUdnWDdna2tHb0toIiwidHlwIjoiSldUIn0.eyJhbXIiOlsiZW1haWwiXSwiZHJuIjoiRFNSIiwiZHYiOjEsImV4cCI6MTc0NTc1NzgwOSwiaWF0IjoxNzQzMzM4NjA5LCJpc3MiOiJQMnVsUTZWS3JReDdkcWw0ZHZ6WnZDTWViR1NQIiwic3ViIjoiVTJ2Mk1GQXhUeGxxYllRYWs2d3E3V1lJR0VkaSJ9.O3zDR5QLr-GwNUwdp26D-5llgD4TMXwgfTW7Dtox8jeVfTQL5TrUhLPzUzyxJCSVyOkKNuoxI8NA0r1iKIY9q0y65ygik4gYZPJhQn2poEOKEiO_bk0LNkZ0GiFzCcmdiUVNe5bkFeTnzP7fhLxSacDkidOSRXx2oVxjcOt5ihqSamluPKpoGwAZz5SZpXvVwTxEVbf6m0LuqAB1Rq2qB4cIC83FDmRgeQymYkV5ULKPNrPDOCpbKok9mJE9nsBgRSV8QQPm7L_9Im8wooGYfarVJIfEC8YYbr5axiQlAhGxaWDMOoGSHNFvrak_bQu_mXF6DJFxPw8uhnlNLhGGyQ";
            client.PostResponse = new
            {
                sessionJwt,
                refreshJwt,
                cookieDomain = "",
                cookiePath = "/",
                cookieMaxAge = 2419199,
                cookieExpiration = 1745757809,
                user = new
                {
                    loginIds = new[] { "tester+4d9dea53-54a1-47ec-8bef-156e8a5bdd4c@descope.com" },
                    userId = "U2v2MFAxTxlqbYQak6wq7WYIGEdi",
                    name = "Unit Tester",
                    email = "tester+4d9dea53-54a1-47ec-8bef-156e8a5bdd4c@descope.com",
                    phone = "",
                    verifiedEmail = true,
                    verifiedPhone = false,
                    roleNames = new string[] { },
                    userTenants = new object[] { },
                    status = "enabled",
                    externalIds = new[] { "tester+4d9dea53-54a1-47ec-8bef-156e8a5bdd4c@descope.com" },
                    picture = "",
                    test = false,
                    customAttributes = new { },
                    createdTime = 1743338609,
                    TOTP = false,
                    SAML = false,
                    OAuth = new { },
                    webauthn = false,
                    password = false,
                    ssoAppIds = new string[] { },
                    givenName = "",
                    middleName = "",
                    familyName = "",
                    SCIM = false
                },
                firstSeen = true,
                sessionExpiration = 1743339209
            };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.EnchantedLinkGetSession, url);
                Assert.Null(pswd);
                EnchantedLink.GetSessionRequest requestBody = Utils.Convert<EnchantedLink.GetSessionRequest>(body);
                Assert.Equal("pendingRef", requestBody.PendingRef);
                return null;
            };
            var session = await enchanted.GetSession("pendingRef");
            Assert.True(session.FirstSeen);
            Assert.Equal("U2v2MFAxTxlqbYQak6wq7WYIGEdi", session.User.UserId);
            Assert.Equal("Unit Tester", session.User.Name);
            Assert.Equal(refreshJwt, session.RefreshToken.Jwt);
            Assert.Equal(sessionJwt, session.SessionToken.Jwt);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task EnchantedLink_Verify()
        {
            var client = new MockHttpClient();
            IEnchantedLink enchanted = new EnchantedLink(client);
            client.PostResponse = new { };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.EnchantedLinkVerify, url);
                Assert.Null(pswd);
                EnchantedLink.VerifyRequest requestBody = Utils.Convert<EnchantedLink.VerifyRequest>(body);
                Assert.Equal("token", requestBody.Token);
                return null;
            };
            await enchanted.Verify("token");
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task EnchantedLink_UpdateEmail()
        {
            var client = new MockHttpClient();
            IEnchantedLink enchanted = new EnchantedLink(client);
            client.PostResponse = new { maskedEmail = "maskedEmail" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.EnchantedLinkUpdateEmail, url);
                Assert.Equal("refreshJwt", pswd);
                EnchantedLink.UpdateEmailRequest requestBody = Utils.Convert<EnchantedLink.UpdateEmailRequest>(body);
                Assert.Equal("loginId", requestBody.LoginId);
                Assert.Equal("pickachu@pokemans.org", requestBody.Email);
                Assert.Equal("uri", requestBody.URI);
                Assert.True(requestBody.AddToLoginIds);
                return null;
            };
            var updateOptions = new UpdateOptions { AddToLoginIds = true };
            var response = await enchanted.UpdateUserEmail("loginId", "pickachu@pokemans.org", "uri", updateOptions, null, "refreshJwt");
            Assert.Equal("maskedEmail", response.MaskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task EnchantedLink_UpdateEmail_BadAddress()
        {
            var client = new MockHttpClient();
            IEnchantedLink enchanted = new EnchantedLink(client);
            await AssertInvalidFormat(enchanted, "nope");
            await AssertInvalidFormat(enchanted, "nope@");
            await AssertInvalidFormat(enchanted, "@nope");
            await AssertInvalidFormat(enchanted, "@");
            await AssertInvalidFormat(enchanted, "");
            await AssertInvalidFormat(enchanted, null);
            await AssertInvalidFormat(enchanted, " @nope.com");

            static async Task AssertInvalidFormat(IEnchantedLink enchanted, string email)
            {
                try
                {
                    var response = await enchanted.UpdateUserEmail("loginId", email, "uri", null, null, "refreshJwt");
                    Assert.True(false, "Should have thrown an exception");
                }
                catch (ArgumentException)
                {
                }
            }
        }

    }

}
