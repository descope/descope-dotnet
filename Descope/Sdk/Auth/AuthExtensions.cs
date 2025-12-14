using Descope.Auth.Models.Onetimev1;
using Microsoft.Kiota.Abstractions;

namespace Descope;

/// <summary>
/// Extension methods for authentication operations that require Refresh JWT tokens in the request's authorization header.
/// These methods provide a cleaner API by making the refresh JWT parameter explicit and mandatory,
/// preventing accidental omission of required authentication context.
/// </summary>
public static class AuthExtensions
{

#pragma warning disable CS0618 // Type or member is obsolete - Allow calling "internally" deprecated Kiota methods
    #region Private Helper Methods

    /// <summary>
    /// Creates a request configuration action that adds a JWT token to the authorization context.
    /// Use this when calling API methods that require a JWT (e.g., refresh token for update operations).
    /// </summary>
    /// <param name="jwt">The JWT token to include in the authorization header.</param>
    /// <returns>An action that configures the request with the JWT token.</returns>
    private static Action<RequestConfiguration<DefaultQueryParameters>> WithJwt(string jwt)
    {
        if (string.IsNullOrEmpty(jwt))
        {
            throw new DescopeException("JWT cannot be empty");
        }

        return requestConfiguration =>
        {
            requestConfiguration.Options.Add(DescopeJwtOption.WithJwt(jwt));
        };
    }

    /// <summary>
    /// Creates a request configuration action that adds an access key to the authorization context.
    /// Use this when calling API methods that require an access key (e.g., access key exchange).
    /// Unlike JWT authentication, this does not trigger appending the auth management key (if configured).
    /// </summary>
    /// <param name="key">The access key to include in the authorization header.</param>
    /// <returns>An action that configures the request with the access key.</returns>
    private static Action<RequestConfiguration<DefaultQueryParameters>> WithKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new DescopeException("Access key cannot be empty");
        }

        return requestConfiguration =>
        {
            requestConfiguration.Options.Add(DescopeKeyOption.WithKey(key));
        };
    }

    #endregion

    #region Magiclink Update Extensions

    /// <summary>
    /// Updates user email using magic link with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The email request builder.</param>
    /// <param name="request">The update email request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The update email response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    /// <example>
    /// <code>
    /// var response = await client.Auth.V1.Magiclink.Update.Email.PostWithJwtAsync(
    ///     new UpdateUserEmailMagicLinkRequest { ... },
    ///     refreshToken
    /// );
    /// </code>
    /// </example>
    public static async Task<EmailMagicLinkResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Magiclink.Update.Email.EmailRequestBuilder requestBuilder,
        UpdateUserEmailMagicLinkRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user email");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    /// <summary>
    /// Updates user phone using magic link SMS with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The SMS request builder.</param>
    /// <param name="request">The update phone request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The update phone response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<PhoneMagicLinkResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Magiclink.Update.Phone.Sms.SmsRequestBuilder requestBuilder,
        UpdateUserPhoneMagicLinkRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user phone");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    /// <summary>
    /// Updates user phone using magic link WhatsApp with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The WhatsApp request builder.</param>
    /// <param name="request">The update phone request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The update phone response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<PhoneMagicLinkResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Magiclink.Update.Phone.Whatsapp.WhatsappRequestBuilder requestBuilder,
        UpdateUserPhoneMagicLinkRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user phone");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region Enchantedlink Update Extensions

    /// <summary>
    /// Updates user email using enchanted link with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The email request builder.</param>
    /// <param name="request">The update email request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enchanted link response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<EnchantedLinkResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Enchantedlink.Update.Email.EmailRequestBuilder requestBuilder,
        UpdateUserEmailEnchantedLinkRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user email");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region OTP Update Extensions

    /// <summary>
    /// Updates user email using OTP with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The email request builder.</param>
    /// <param name="request">The update email request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The email operation response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<EmailOperationResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Otp.Update.Email.EmailRequestBuilder requestBuilder,
        UpdateUserEmailOTPRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user email");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    /// <summary>
    /// Updates user phone using OTP SMS with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The SMS request builder.</param>
    /// <param name="request">The update phone request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The phone operation response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<PhoneOperationResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Otp.Update.Phone.Sms.SmsRequestBuilder requestBuilder,
        UpdateUserPhoneOTPRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user phone");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    /// <summary>
    /// Updates user phone using OTP Voice with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The Voice request builder.</param>
    /// <param name="request">The update phone request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The phone operation response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<PhoneOperationResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Otp.Update.Phone.Voice.VoiceRequestBuilder requestBuilder,
        UpdateUserPhoneOTPRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user phone");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    /// <summary>
    /// Updates user phone using OTP WhatsApp with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The WhatsApp request builder.</param>
    /// <param name="request">The update phone request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The phone operation response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<PhoneOperationResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Otp.Update.Phone.Whatsapp.WhatsappRequestBuilder requestBuilder,
        UpdateUserPhoneOTPRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user phone");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region Password Update Extensions

    /// <summary>
    /// Updates user password with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The update request builder.</param>
    /// <param name="request">The password update request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<Stream?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Password.Update.UpdateRequestBuilder requestBuilder,
        PasswordUpdateRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating user password");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region TOTP Update Extensions

    /// <summary>
    /// Updates TOTP (Time-based One-Time Password) for a user with mandatory JWT authentication.
    /// Creates a seed for an existing user so they can use an authenticator app.
    /// </summary>
    /// <param name="requestBuilder">The update request builder.</param>
    /// <param name="request">The TOTP update request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The TOTP response containing QR code and provisioning URL.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<TOTPResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Totp.Update.UpdateRequestBuilder requestBuilder,
        TOTPUpdateRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating TOTP");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region WebAuthn Update Extensions

    /// <summary>
    /// Starts the WebAuthn device update process with mandatory JWT authentication.
    /// Adds a new WebAuthn device (e.g., security key, passkey) to an existing user.
    /// </summary>
    /// <param name="requestBuilder">The start request builder.</param>
    /// <param name="request">The WebAuthn add device start request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The WebAuthn start response containing transaction ID and options.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<WebauthnStartResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Webauthn.Update.Start.StartRequestBuilder requestBuilder,
        WebauthnAddDeviceStartRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for updating WebAuthn device");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region Session Refresh Extensions

    /// <summary>
    /// Refreshes a session using a refresh JWT with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The refresh request builder.</param>
    /// <param name="request">The refresh session request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JWT response containing new session tokens.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<JWTResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Refresh.RefreshRequestBuilder requestBuilder,
        RefreshSessionRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for refreshing session");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region Access Key Exchange Extensions

    /// <summary>
    /// Exchanges an access key for a session JWT with mandatory access key authentication.
    /// </summary>
    /// <param name="requestBuilder">The exchange request builder.</param>
    /// <param name="request">The exchange access key request.</param>
    /// <param name="accessKey">The access key (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exchange access key response containing session tokens.</returns>
    /// <exception cref="DescopeException">Thrown when accessKey is null or empty.</exception>
    public static async Task<ExchangeAccessKeyResponse?> PostWithKeyAsync(
        this Descope.Auth.V1.Auth.Accesskey.Exchange.ExchangeRequestBuilder requestBuilder,
        ExchangeAccessKeyRequest request,
        string accessKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(accessKey))
        {
            throw new DescopeException("Access key is required for exchange");
        }

        return await requestBuilder.PostAsync(
            request,
            WithKey(accessKey),
            cancellationToken);
    }

    #endregion

    #region Me Extensions

    /// <summary>
    /// Gets the user details for the current refresh token with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The Me request builder.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user details.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<Descope.Auth.Models.Userv1.ResponseUser?> GetWithJwtAsync(
        this Descope.Auth.V1.Auth.Me.MeRequestBuilder requestBuilder,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for getting user details");
        }

        return await requestBuilder.GetAsync(
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region Logout Extensions

    /// <summary>
    /// Logs out from the session with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The Logout request builder.</param>
    /// <param name="request">The logout request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JWT response.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<JWTResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Logout.LogoutRequestBuilder requestBuilder,
        LogoutRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for logout");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region Tenant Selection Extensions

    /// <summary>
    /// Selects a tenant for the current session with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The Select request builder.</param>
    /// <param name="request">The select tenant request.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JWT response with new session tokens for the selected tenant.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    public static async Task<JWTResponse?> PostWithJwtAsync(
        this Descope.Auth.V1.Auth.Tenant.Select.SelectRequestBuilder requestBuilder,
        SelectTenantRequest request,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for selecting tenant");
        }

        return await requestBuilder.PostAsync(
            request,
            WithJwt(refreshJwt),
            cancellationToken);
    }

    #endregion

    #region Auth History Extensions

    /// <summary>
    /// Gets the user authentication history for the current refresh token with mandatory JWT authentication.
    /// </summary>
    /// <param name="requestBuilder">The History request builder.</param>
    /// <param name="refreshJwt">The refresh JWT token (required for this operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The auth history response containing authentication events.</returns>
    /// <exception cref="DescopeException">Thrown when refreshJwt is null or empty.</exception>
    /// <example>
    /// <code>
    /// var history = await client.Auth.V1.Me.History.GetWithJwtAsync(refreshToken);
    /// </code>
    /// </example>
    public static async Task<MeAuthHistoryResponse?> GetWithJwtAsync(
        this Descope.Auth.V1.Auth.Me.History.HistoryRequestBuilder requestBuilder,
        string refreshJwt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT is required for this operation.");
        }

        return await requestBuilder.GetAsync(
            WithJwt(refreshJwt),
            cancellationToken
        );
    }

    #endregion

    #region SSO Authorize Extensions

    /// <summary>
    /// Initiates SSO authentication flow with comprehensive query parameters.
    /// Creates a redirect URL for SSO authentication based on tenant configuration (SAML/OIDC).
    /// </summary>
    /// <param name="requestBuilder">The Authorize request builder.</param>
    /// <param name="request">The login options request.</param>
    /// <param name="tenant">The tenant ID (required for SSO authentication).</param>
    /// <param name="redirectUrl">The URL to redirect to after authentication.</param>
    /// <param name="prompt">Optional OIDC prompt parameter (e.g., "login", "consent", "none").</param>
    /// <param name="loginHint">Optional login hint sent to the IdP (e.g., pre-fill username).</param>
    /// <param name="forceAuthn">Optional flag to force re-authentication even if user has active session.</param>
    /// <param name="test">Optional flag to enable test mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SAML redirect response containing the SSO redirect URL.</returns>
    /// <exception cref="DescopeException">Thrown when tenant is null or empty.</exception>
    /// <example>
    /// <code>
    /// var response = await client.Auth.V1.Sso.Authorize.PostWithQueryParamsAsync(
    ///     new LoginOptions(),
    ///     tenant: "my-tenant-id",
    ///     redirectUrl: "https://myapp.com/callback",
    ///     test: true
    /// );
    /// </code>
    /// </example>
    public static async Task<Auth.Models.Onetimev1.SAMLRedirectResponse?> PostWithQueryParamsAsync(
        this Descope.Auth.V1.Auth.Sso.Authorize.AuthorizeRequestBuilder requestBuilder,
        Auth.Models.Onetimev1.LoginOptions request,
        string tenant,
        string? redirectUrl = null,
        string[]? prompt = null,
        string? loginHint = null,
        bool? forceAuthn = null,
        bool? test = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenant))
        {
            throw new DescopeException("Tenant is required for SSO authorization.");
        }

        return await requestBuilder.PostAsync(
            request,
            requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Tenant = tenant;

                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    requestConfiguration.QueryParameters.RedirectUrl = redirectUrl;
                }

                if (prompt != null && prompt.Length > 0)
                {
                    requestConfiguration.QueryParameters.Prompt = prompt;
                }

                if (!string.IsNullOrEmpty(loginHint))
                {
                    requestConfiguration.QueryParameters.LoginHint = loginHint;
                }

                if (forceAuthn.HasValue)
                {
                    requestConfiguration.QueryParameters.ForceAuthn = forceAuthn.Value;
                }

                if (test.HasValue)
                {
                    requestConfiguration.QueryParameters.Test = test.Value;
                }
            },
            cancellationToken);
    }

    #endregion
#pragma warning restore CS0618
}
