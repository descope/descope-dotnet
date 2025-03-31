using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using Microsoft.IdentityModel.JsonWebTokens;

[assembly: InternalsVisibleTo("Descope.Test")] // expose request bodies for unit testing

namespace Descope.Internal.Auth
{
    public class EnchantedLink : IEnchantedLink
    {
        private readonly IHttpClient _httpClient;

        private readonly JsonWebTokenHandler _jsonWebTokenHandler = new();

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

        public async Task<Session> GetSession(string pendingRef)
        {
            if (string.IsNullOrEmpty(pendingRef))
                throw new ArgumentException("pendingRef cannot be empty", nameof(pendingRef));

            var body = new GetSessionRequest
            {
                PendingRef = pendingRef
            };

            var response = await _httpClient.Post<AuthenticationResponse>(
                Routes.EnchantedLinkGetSession,
                body: body) ?? throw new DescopeException("Failed to get session from enchanted link response");

            var sessionToken = new Token(_jsonWebTokenHandler.ReadJsonWebToken(response.SessionJwt));
            var refreshToken = new Token(_jsonWebTokenHandler.ReadJsonWebToken(response.RefreshJwt));
            return new Session(
                sessionToken: sessionToken,
                refreshToken: refreshToken,
                user: response.User,
                firstSeen: response.FirstSeen
            );
        }

        public async Task Verify(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("token cannot be empty", nameof(token));

            var body = new VerifyRequest
            {
                Token = token
            };

            await _httpClient.Post<object>(
                Routes.EnchantedLinkVerify,
                body: body);
        }

        public async Task<EnchantedLinkResponse> UpdateUserEmail(
    string loginId,
    string email,
    string? uri,
    UpdateOptions? updateOptions,
    Dictionary<string, string>? templateOptions,
    string refreshJwt)
        {
            if (string.IsNullOrEmpty(loginId))
                throw new ArgumentException("loginId cannot be empty", nameof(loginId));
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("email cannot be empty", nameof(email));
            if (!Utils.IsValidEmail(email))
                throw new ArgumentException("email format is invalid", nameof(email));
            if (string.IsNullOrEmpty(refreshJwt))
                throw new ArgumentException("Refresh JWT is required", nameof(refreshJwt));

            updateOptions ??= new UpdateOptions();

            var body = new UpdateEmailRequest
            {
                LoginId = loginId,
                Email = email,
                URI = uri,
                AddToLoginIds = updateOptions.AddToLoginIds,
                OnMergeUseExisting = updateOptions.OnMergeUseExisting,
                TemplateOptions = templateOptions
            };

            var response = await _httpClient.Post<EnchantedLinkResponse>(
                Routes.EnchantedLinkUpdateEmail,
                body: body,
                pswd: refreshJwt);

            return response;
        }

        // Request bodies for enchanted link API calls

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

        internal record GetSessionRequest
        {
            [JsonPropertyName("pendingRef")]
            public string PendingRef { get; init; }
        }

        internal record VerifyRequest
        {
            [JsonPropertyName("token")]
            public string Token { get; init; }
        }

        internal record UpdateEmailRequest : BaseAuthenticationRequest
        {
            [JsonPropertyName("email")]
            public string Email { get; init; }

            [JsonPropertyName("addToLoginIds")]
            public bool AddToLoginIds { get; init; }

            [JsonPropertyName("onMergeUseExisting")]
            public bool OnMergeUseExisting { get; init; }

            [JsonPropertyName("templateOptions")]
            public Dictionary<string, string>? TemplateOptions { get; init; }
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    }

}
