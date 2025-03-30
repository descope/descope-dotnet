using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Descope.Test")] // expose request bodies for unit testing

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

            var body = new SignInRequest
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

            var body = new SignUpRequest
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

        public async Task<EnchantedLinkResponse> SignUpOrIn(string loginId, string? uri, SignUpOptions? signUpOptions = null)
        {
            if (string.IsNullOrEmpty(loginId))
                throw new ArgumentException("loginId cannot be empty", nameof(loginId));

            signUpOptions ??= new SignUpOptions();

            var body = new SignUpOrInRequest
            {
                LoginId = loginId,
                URI = uri,
                LoginOptions = signUpOptions
            };

            var response = await _httpClient.Post<EnchantedLinkResponse>(
                Routes.EnchantedLinkSignUpOrIn,
                body: body);

            return response;
        }

        // Disable this warning since we cannot use 'required' without breaking backwards compatibility
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal abstract record BaseAuthenticationRequest
        {
            [JsonPropertyName("loginId")]
            public string LoginId { get; init; } // required
            [JsonPropertyName("URI")]
            public string? URI { get; init; }
            [JsonPropertyName("crossDevice")]
            public bool CrossDevice { get; } = true; // always true for enchanted links
        }

        internal record SignInRequest : BaseAuthenticationRequest
        {
            [JsonPropertyName("loginOptions")]
            public Dictionary<string, object?>? LoginOptions { get; init; }
        }

        internal record SignUpOrInRequest : BaseAuthenticationRequest
        {
            [JsonPropertyName("loginOptions")]
            public SignUpOptions LoginOptions { get; init; }
        }

        internal record SignUpRequest : SignUpOrInRequest
        {
            [JsonPropertyName("user")]
            public SignUpDetails User { get; init; }
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }

}
