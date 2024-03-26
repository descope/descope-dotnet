using System.Text.Json.Serialization;

namespace Descope.Internal.Management
{
    internal class Jwt : IJwt
    {
        private readonly IHttpClient _httpClient;
        private readonly string _managementKey;

        internal Jwt(IHttpClient httpClient, string managementKey)
        {
            _httpClient = httpClient;
            _managementKey = managementKey;
        }

        public async Task<string> UpdateJwtWithCustomClaims(string jwt, Dictionary<string, object> customClaims)
        {
            if (string.IsNullOrEmpty(jwt)) throw new DescopeException("JWT is required to update custom claims");
            var body = new { jwt, customClaims };
            var response = await _httpClient.Post<SimpleJwtResponse>(Routes.JwtUpdate, _managementKey, body);
            return response.Jwt;
        }

        public async Task<string> Impersonate(string impersonatorId, string loginId, bool validateConcent)
        {
            if (string.IsNullOrEmpty(impersonatorId)) throw new DescopeException("impersonatorId is required to impersonate");
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("impersonatorId is required to impersonate");
            var body = new { impersonatorId, loginId, validateConcent };
            var response = await _httpClient.Post<SimpleJwtResponse>(Routes.Impersonate, _managementKey, body);
            return response.Jwt;
        }

    }

    internal class SimpleJwtResponse
    {
        [JsonPropertyName("jwt")]
        public string Jwt { get; set; }

        public SimpleJwtResponse(string jwt)
        {
            Jwt = jwt;
        }
    }

}
