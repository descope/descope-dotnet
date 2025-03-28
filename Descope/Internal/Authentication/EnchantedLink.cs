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
                //queryParams: queryParams,
                pswd: refreshJwt);

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
}
