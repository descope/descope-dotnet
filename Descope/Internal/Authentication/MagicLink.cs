using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("Descope.Test")] // expose request bodies for unit testing

namespace Descope.Internal.Auth
{
    public class MagicLink : IMagicLink
    {
        private readonly IHttpClient _httpClient;

        public MagicLink(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SignIn(DeliveryMethod deliveryMethod, string loginId, string? uri = null, LoginOptions? loginOptions = null, string? refreshJwt = null)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");

            if (loginOptions != null && loginOptions.IsJWTRequired && string.IsNullOrEmpty(refreshJwt))
                throw new DescopeException("Refresh JWT is required for stepup or MFA");

            var body = new SignInRequest
            {
                LoginId = loginId,
                URI = uri,
                LoginOptions = loginOptions?.ToDictionary()
            };

            var response = await _httpClient.Post<MaskedAddressResponse>(
                Routes.MagicLinkSignIn + deliveryMethod.ToString().ToLower(),
                pswd: refreshJwt,
                body: body);

            return deliveryMethod == DeliveryMethod.Email ? response.MaskedEmail ?? "" : response.MaskedPhone ?? "";
        }

        public async Task<string> SignUp(DeliveryMethod deliveryMethod, string loginId, string? uri = null, SignUpDetails? signUpDetails = null, SignUpOptions? signUpOptions = null)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");

            signUpDetails ??= new SignUpDetails();
            signUpOptions ??= new SignUpOptions();

            // Set email or phone based on delivery method
            if (deliveryMethod == DeliveryMethod.Email)
            {
                if (string.IsNullOrEmpty(signUpDetails.Email))
                    signUpDetails.Email = loginId;
            }
            else // SMS or WhatsApp
            {
                if (string.IsNullOrEmpty(signUpDetails.Phone))
                    signUpDetails.Phone = loginId;
            }

            var body = new SignUpRequest
            {
                LoginId = loginId,
                URI = uri,
                User = signUpDetails,
                LoginOptions = signUpOptions
            };

            var response = await _httpClient.Post<MaskedAddressResponse>(
                Routes.MagicLinkSignUp + deliveryMethod.ToString().ToLower(),
                body: body);

            return deliveryMethod == DeliveryMethod.Email ? response.MaskedEmail ?? "" : response.MaskedPhone ?? "";
        }

        public async Task<string> SignUpOrIn(DeliveryMethod deliveryMethod, string loginId, string? uri = null, SignUpOptions? signUpOptions = null)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");

            signUpOptions ??= new SignUpOptions();

            var body = new SignUpOrInRequest
            {
                LoginId = loginId,
                URI = uri,
                LoginOptions = signUpOptions
            };

            var response = await _httpClient.Post<MaskedAddressResponse>(
                Routes.MagicLinkSignUpOrIn + deliveryMethod.ToString().ToLower(),
                body: body);

            return deliveryMethod == DeliveryMethod.Email ? response.MaskedEmail ?? "" : response.MaskedPhone ?? "";
        }

        public async Task<AuthenticationResponse> Verify(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new DescopeException("token missing");
            var body = new VerifyRequest { Token = token };
            return await _httpClient.Post<AuthenticationResponse>(Routes.MagicLinkVerify, null, body);
        }

        // Request bodies for magic link API calls

        // Disable this warning since we cannot use 'required' without breaking backwards compatibility
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        internal record SignInRequest
        {
            [JsonPropertyName("loginId")]
            public string LoginId { get; init; }

            [JsonPropertyName("URI")]
            public string? URI { get; init; }

            [JsonPropertyName("loginOptions")]
            public Dictionary<string, object?>? LoginOptions { get; init; }
        }

        internal record SignUpOrInRequest
        {
            [JsonPropertyName("loginId")]
            public string LoginId { get; init; }

            [JsonPropertyName("URI")]
            public string? URI { get; init; }

            [JsonPropertyName("loginOptions")]
            public SignUpOptions LoginOptions { get; init; }
        }

        internal record SignUpRequest : SignUpOrInRequest
        {
            [JsonPropertyName("user")]
            public SignUpDetails User { get; init; }
        }

        internal record VerifyRequest
        {
            [JsonPropertyName("token")]
            public string Token { get; init; }
        }

        internal class MaskedAddressResponse
        {
            [JsonPropertyName("maskedEmail")]
            public string? MaskedEmail { get; set; }

            [JsonPropertyName("maskedPhone")]
            public string? MaskedPhone { get; set; }
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
}
