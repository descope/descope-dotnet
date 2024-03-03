namespace Descope.Internal.Auth
{
    public class Otp : IOtp
    {
        private readonly IHttpClient _httpClient;

        public Otp(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SignUp(DeliveryMethod deliveryMethod, string loginId, SignUpDetails? details)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var body = new { loginId, user = details };
            var response = await _httpClient.Post<MaskedAddressResponse>(Routes.OtpSignUp + deliveryMethod.ToString().ToLower(), body: body);
            return deliveryMethod == DeliveryMethod.Email ? response.MaskedEmail ?? "" : response.MaskedPhone ?? "";
        }

        public async Task<string> SignIn(DeliveryMethod deliveryMethod, string loginId, LoginOptions? loginOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var body = new { loginId, loginOptions };
            var response = await _httpClient.Post<MaskedAddressResponse>(Routes.OtpSignIn + deliveryMethod.ToString().ToLower(), body: body);
            return deliveryMethod == DeliveryMethod.Email ? response.MaskedEmail ?? "" : response.MaskedPhone ?? "";
        }

        public async Task<string> SignUpOrIn(DeliveryMethod deliveryMethod, string loginId, LoginOptions? loginOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var body = new { loginId, loginOptions };
            var response = await _httpClient.Post<MaskedAddressResponse>(Routes.OtpSignUpOrIn + deliveryMethod.ToString().ToLower(), body: body);
            return deliveryMethod == DeliveryMethod.Email ? response.MaskedEmail ?? "" : response.MaskedPhone ?? "";
        }

        public async Task<AuthenticationResponse> Verify(DeliveryMethod deliveryMethod, string loginId, string code)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            var body = new { loginId, code };
            return await _httpClient.Post<AuthenticationResponse>(Routes.OtpVerify + deliveryMethod.ToString().ToLower(), body: body);
        }

        public async Task<string> UpdateEmail(string loginId, string email, string refreshJwt, UpdateOptions? updateOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            if (string.IsNullOrEmpty(email)) throw new DescopeException("email missing");
            if (string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("refreshJwt missing");
            var body = new
            {
                loginId,
                email,
                addToLoginIDs = updateOptions?.AddToLoginIds,
                onMergeUseExisting = updateOptions?.OnMergeUseExisting,
            };
            var result = await _httpClient.Post<MaskedAddressResponse>(Routes.OtpUpdateEmail, refreshJwt, body);
            return result.MaskedEmail ?? "";
        }

        public async Task<string> UpdatePhone(string loginId, string phone, string refreshJwt, UpdateOptions? updateOptions)
        {
            if (string.IsNullOrEmpty(loginId)) throw new DescopeException("loginId missing");
            if (string.IsNullOrEmpty(phone)) throw new DescopeException("phone missing");
            if (string.IsNullOrEmpty(refreshJwt)) throw new DescopeException("refreshJwt missing");
            var body = new
            {
                loginId,
                phone,
                addToLoginIDs = updateOptions?.AddToLoginIds,
                onMergeUseExisting = updateOptions?.OnMergeUseExisting,
            };
            var response = await _httpClient.Post<MaskedAddressResponse>(Routes.OtpUpdatePhone, refreshJwt, body);
            return response.MaskedPhone ?? "";
        }

    }
}
