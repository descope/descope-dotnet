using System.Runtime.CompilerServices;

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

        public async Task<AuthenticationResponse> Verify(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new DescopeException("token missing");
            var body = new { token };
            return await _httpClient.Post<AuthenticationResponse>(Routes.MagicLinkVerify, null, body);
        }
    }
}