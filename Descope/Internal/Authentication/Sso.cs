using System.Text.Json.Serialization;

namespace Descope.Internal.Auth
{
    public class Sso : ISsoAuth
    {
        private readonly IHttpClient _httpClient;

        public Sso(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> Start(string tenant, string? redirectUrl, string? prompt, bool? forceAuthn, LoginOptions? loginOptions)
        {
            Utils.EnforceRequiredArgs(("tenant", tenant));
            var body = new { loginOptions = loginOptions?.ToDictionary() };
            var queryParams = new Dictionary<string, string?> { { "tenant", tenant }, { "redirectUrl", redirectUrl }, { "prompt", prompt }};
            
            if (forceAuthn.HasValue)
            {
                queryParams["forceAuthn"] = forceAuthn.Value.ToString().ToLower();
            }
            
            var response = await _httpClient.Post<UrlResponse>(Routes.SsoStart, body: body, queryParams: queryParams, pswd: loginOptions?.GetRefreshJwt());
            return response.Url;
        }

        public async Task<AuthenticationResponse> Exchange(string code)
        {
            Utils.EnforceRequiredArgs(("code", code));
            var body = new { code };
            return await _httpClient.Post<AuthenticationResponse>(Routes.SsoExchange, body: body);
        }
    }

}
