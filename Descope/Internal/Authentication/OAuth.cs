using System.Text.Json.Serialization;

namespace Descope.Internal.Auth
{
    public class OAuth : IOAuth
    {
        private readonly IHttpClient _httpClient;

        public OAuth(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SignUpOrIn(string provider, string? redirectUrl = null, LoginOptions? loginOptions = null)
        {
            var body = new { loginOptions = loginOptions?.ToDictionary() };
            var queryParams = new Dictionary<string, string?> { { "provider", provider }, { "redirectUrl", redirectUrl } };
            var response = await _httpClient.Post<UrlResponse>(Routes.OAuthSignUpOrIn, body: body, queryParams: queryParams, pswd: loginOptions?.GetRefreshJwt());
            return response.Url;
        }

        public async Task<string> SignUp(string provider, string? redirectUrl = null, LoginOptions? loginOptions = null)
        {
            var body = new { loginOptions = loginOptions?.ToDictionary() };
            var queryParams = new Dictionary<string, string?> { { "provider", provider }, { "redirectUrl", redirectUrl } };
            var response = await _httpClient.Post<UrlResponse>(Routes.OAuthSignUp, body: body, queryParams: queryParams, pswd: loginOptions?.GetRefreshJwt());
            return response.Url;
        }

        public async Task<string> SignIn(string provider, string? redirectUrl = null, LoginOptions? loginOptions = null)
        {
            var body = new { loginOptions = loginOptions?.ToDictionary() };
            var queryParams = new Dictionary<string, string?> { { "provider", provider }, { "redirectUrl", redirectUrl } };
            var response = await _httpClient.Post<UrlResponse>(Routes.OAuthSignIn, body: body, queryParams: queryParams, pswd: loginOptions?.GetRefreshJwt());
            return response.Url;
        }

        public async Task<string> UpdateUser(string provider, string refreshJwt, string? redirectUrl = null, bool allowAllMerge = false, LoginOptions? loginOptions = null)
        {
            var body = new { loginOptions = loginOptions?.ToDictionary() };
            var queryParams = new Dictionary<string, string?> { { "provider", provider }, { "redirectUrl", redirectUrl }, { "allowAllMerge", allowAllMerge.ToString().ToLower() } };
            var response = await _httpClient.Post<UrlResponse>(Routes.OAuthUpdate, body: body, queryParams: queryParams, pswd: refreshJwt);
            return response.Url;
        }

        public async Task<AuthenticationResponse> Exchange(string code)
        {
            var body = new { code };
            return await _httpClient.Post<AuthenticationResponse>(Routes.OAuthExchange, body: body);
        }
    }

}
