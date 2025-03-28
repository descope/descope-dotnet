using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Descope.Internal.Auth
{
    public class EnchantedLink : IEnchantedLink
    {
        private readonly IHttpClient _httpClient;

        public EnchantedLink(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EnchantedLinkResponse> SignIn(string loginId, string? uri, LoginOptions? loginOptions = null, string? refreshJwt = null)
        {
            if (string.IsNullOrEmpty(loginId))
                throw new ArgumentException("loginId cannot be empty", nameof(loginId));

            if (loginOptions != null && loginOptions.IsJWTRequired && string.IsNullOrEmpty(refreshJwt))
                throw new ArgumentException("Refresh JWT is required", nameof(refreshJwt));

            var body = new EnchantedLinkAuthenticationRequestBody
            {
                LoginId = loginId,
                URI = uri,
                LoginOptions = loginOptions?.ToDictionary()
            };

            var response = await _httpClient.Post<EnchantedLinkResponse>(
                Routes.EnchantedLinkSignIn,
                body: body,
                pswd: refreshJwt);

            return response;
        }

        public async Task<EnchantedLinkResponse> SignUp(
            string loginId,
            string? uri,
            SignUpDetails? signUpDetails = null,
            SignUpOptions? signUpOptions = null)
        {
            if (string.IsNullOrEmpty(loginId))
                throw new ArgumentException("loginId cannot be empty", nameof(loginId));

            signUpOptions ??= new SignUpOptions();
            signUpDetails ??= new SignUpDetails();

            if (string.IsNullOrEmpty(signUpDetails.Email))
                signUpDetails.Email = loginId;

            var body = new EnchantedLinkSignUpRequestBody
            {
                LoginId = loginId,
                URI = uri,
                User = signUpDetails,
                LoginOptions = signUpOptions
            };

            var response = await _httpClient.Post<EnchantedLinkResponse>(
                Routes.EnchantedLinkSignUp,
                body: body);

            return response;
        }
    }

    public record EnchantedLinkAuthenticationRequestBody
    {
        [JsonPropertyName("loginId")]
        public string LoginId { get; init; } // required
        [JsonPropertyName("URI")]
        public string? URI { get; init; }
        [JsonPropertyName("crossDevice")]
        public bool CrossDevice { get; } = true; // always true for enchanted links
        [JsonPropertyName("loginOptions")]
        public Dictionary<string, object?>? LoginOptions { get; init; }
    }

    public record EnchantedLinkSignUpRequestBody
    {
        [JsonPropertyName("loginId")]
        public string LoginId { get; init; }
        [JsonPropertyName("URI")]
        public string? URI { get; init; }
        [JsonPropertyName("crossDevice")]
        public bool CrossDevice { get; } = true; // always true for enchanted links
        [JsonPropertyName("user")]
        public SignUpDetails User { get; init; }
        [JsonPropertyName("loginOptions")]
        public SignUpOptions LoginOptions { get; init; }
    }
}
