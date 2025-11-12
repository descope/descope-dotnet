using Descope.Internal;
using Descope.Internal.Auth;
using System.Text.Json;
using Xunit;

namespace Descope.Test.Unit
{
    public class MagicLinkTests
    {
        [Fact]
        public async Task MagicLink_SignIn_Email_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedEmail = "t***@example.com" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignIn + "email", url);
                Assert.Null(pswd);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.Equal("test@example.com", requestBody.GetProperty("loginId").GetString());
                Assert.Equal("https://example.com", requestBody.GetProperty("URI").GetString());
                return null;
            };

            var maskedEmail = await magicLink.SignIn(DeliveryMethod.Email, "test@example.com", "https://example.com");

            Assert.Equal("t***@example.com", maskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignIn_SMS_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedPhone = "+1***1234" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignIn + "sms", url);
                Assert.Null(pswd);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.Equal("+11234567890", requestBody.GetProperty("loginId").GetString());
                return null;
            };

            var maskedPhone = await magicLink.SignIn(DeliveryMethod.Sms, "+11234567890");

            Assert.Equal("+1***1234", maskedPhone);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignIn_WithLoginOptions_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedEmail = "t***@example.com" };
            var loginOptions = new LoginOptions { CustomClaims = new Dictionary<string, object> { { "key", "value" } } };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignIn + "email", url);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.True(requestBody.GetProperty("loginOptions").TryGetProperty("customClaims", out JsonElement customClaims));
                return null;
            };

            var maskedEmail = await magicLink.SignIn(DeliveryMethod.Email, "test@example.com", loginOptions: loginOptions);

            Assert.Equal("t***@example.com", maskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignIn_ThrowsForEmptyLoginId()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);

            async Task Act() => await magicLink.SignIn(DeliveryMethod.Email, "");
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Equal("loginId missing", exception.Message);
            Assert.Equal(0, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignUp_Email_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedEmail = "t***@example.com" };
            var signUpDetails = new SignUpDetails { Name = "Test User", Email = "test@example.com" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignUp + "email", url);
                Assert.Null(pswd);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.Equal("test@example.com", requestBody.GetProperty("loginId").GetString());
                Assert.True(requestBody.TryGetProperty("user", out JsonElement user));
                Assert.Equal("Test User", user.GetProperty("name").GetString());
                return null;
            };

            var maskedEmail = await magicLink.SignUp(DeliveryMethod.Email, "test@example.com", signUpDetails: signUpDetails);

            Assert.Equal("t***@example.com", maskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignUp_SMS_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedPhone = "+1***1234" };
            var signUpDetails = new SignUpDetails { Name = "Test User", Phone = "+11234567890" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignUp + "sms", url);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.Equal("+11234567890", requestBody.GetProperty("loginId").GetString());
                Assert.True(requestBody.TryGetProperty("user", out JsonElement userProp));
                Assert.Equal("+11234567890", userProp.GetProperty("phone").GetString());
                return null;
            };

            var maskedPhone = await magicLink.SignUp(DeliveryMethod.Sms, "+11234567890", signUpDetails: signUpDetails);

            Assert.Equal("+1***1234", maskedPhone);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignUp_WithSignUpOptions_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedEmail = "t***@example.com" };
            var signUpOptions = new SignUpOptions
            {
                CustomClaims = new Dictionary<string, object> { { "key", "value" } },
                TemplateID = "template123"
            };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignUp + "email", url);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.True(requestBody.GetProperty("loginOptions").TryGetProperty("customClaims", out JsonElement customClaims));
                Assert.Equal("template123", requestBody.GetProperty("loginOptions").GetProperty("templateId").GetString());
                return null;
            };

            var maskedEmail = await magicLink.SignUp(DeliveryMethod.Email, "test@example.com", signUpOptions: signUpOptions);

            Assert.Equal("t***@example.com", maskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignUp_ThrowsForEmptyLoginId()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);

            async Task Act() => await magicLink.SignUp(DeliveryMethod.Email, "");
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Equal("loginId missing", exception.Message);
            Assert.Equal(0, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignUpOrIn_Email_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedEmail = "t***@example.com" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignUpOrIn + "email", url);
                Assert.Null(pswd);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.Equal("test@example.com", requestBody.GetProperty("loginId").GetString());
                return null;
            };

            var maskedEmail = await magicLink.SignUpOrIn(DeliveryMethod.Email, "test@example.com");

            Assert.Equal("t***@example.com", maskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignUpOrIn_WhatsApp_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedPhone = "+1***1234" };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignUpOrIn + "whatsapp", url);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.Equal("+11234567890", requestBody.GetProperty("loginId").GetString());
                return null;
            };

            var maskedPhone = await magicLink.SignUpOrIn(DeliveryMethod.Whatsapp, "+11234567890");

            Assert.Equal("+1***1234", maskedPhone);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignUpOrIn_WithSignUpOptions_Success()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);
            client.PostResponse = new { maskedEmail = "t***@example.com" };
            var signUpOptions = new SignUpOptions { CustomClaims = new Dictionary<string, object> { { "key", "value" } } };
            client.PostAssert = (url, pswd, body, queryParams) =>
            {
                Assert.Equal(Routes.MagicLinkSignUpOrIn + "email", url);
                var requestBody = Utils.Convert<dynamic>(body);
                Assert.True(requestBody.GetProperty("loginOptions").TryGetProperty("customClaims", out JsonElement customClaims));
                return null;
            };

            var maskedEmail = await magicLink.SignUpOrIn(DeliveryMethod.Email, "test@example.com", signUpOptions: signUpOptions);

            Assert.Equal("t***@example.com", maskedEmail);
            Assert.Equal(1, client.PostCount);
        }

        [Fact]
        public async Task MagicLink_SignUpOrIn_ThrowsForEmptyLoginId()
        {
            var client = new MockHttpClient();
            IMagicLink magicLink = new MagicLink(client);

            async Task Act() => await magicLink.SignUpOrIn(DeliveryMethod.Email, "");
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Equal("loginId missing", exception.Message);
            Assert.Equal(0, client.PostCount);
        }

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
