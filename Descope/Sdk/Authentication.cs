namespace Descope
{
    /// <summary>
    /// Provides functions for authenticating users using OTP (one-time password)
    /// </summary>
    public interface IOtp
    {
        Task<string> SignUp(DeliveryMethod deliveryMethod, string loginId, SignUpDetails? details = null);

        Task<string> SignIn(DeliveryMethod deliveryMethod, string loginId, LoginOptions? loginOptions = null);

        Task<string> SignUpOrIn(DeliveryMethod deliveryMethod, string loginId, LoginOptions? loginOptions = null);

        Task<string> UpdateEmail(string loginId, string email, string refreshJwt, UpdateOptions? updateOptions = null);

        Task<string> UpdatePhone(string loginId, string phone, string refreshJwt, UpdateOptions? updateOptions = null);

        Task<AuthenticationResponse> Verify(DeliveryMethod deliveryMethod, string loginId, string code);
    }

    /// <summary>
    /// Authenticate a user using an OAuth provider.
    /// </summary>
    public interface IOAuth
    {
        /// <summary>
        /// Authenticate an existing user if one exists, or create a new user using an OAuth redirect chain.
        /// <para>
        /// A successful authentication will result in a callback to the URL defined here or in the current project settings.
        /// </para>
        /// </summary>
        /// <param name="provider">Which provider to authenticate against</param>
        /// <param name="redirectUrl">Optional redirect URL. If null, the default redirect URL in Descope console will be used.</param>
        /// <param name="loginOptions">Additional behaviors to perform during authentication</param>
        /// <returns>A URL that starts the OAuth redirect chain</returns>
        Task<string> SignUpOrIn(string provider, string? redirectUrl = null, LoginOptions? loginOptions = null);

        /// <summary>
        /// Authenticate a new user using an OAuth redirect chain.
        /// <para>
        /// A successful authentication will result in a callback to the URL defined here or in the current project settings.
        /// </para>
        /// </summary>
        /// <param name="provider">Which provider to authenticate against</param>
        /// <param name="redirectUrl">Optional redirect URL. If null, the default redirect URL in Descope console will be used.</param>
        /// <param name="loginOptions">Additional behaviors to perform during authentication</param>
        /// <returns>A URL that starts the OAuth redirect chain</returns>
        Task<string> SignUp(string provider, string? redirectUrl = null, LoginOptions? loginOptions = null);

        /// <summary>
        /// Authenticate an existing user using an OAuth redirect chain.
        /// <para>
        /// A successful authentication will result in a callback to the URL defined here or in the current project settings.
        /// </para>
        /// </summary>
        /// <param name="provider">Which provider to authenticate against</param>
        /// <param name="redirectUrl">Optional redirect URL. If null, the default redirect URL in Descope console will be used.</param>
        /// <param name="loginOptions">Additional behaviors to perform during authentication</param>
        /// <returns>A URL that starts the OAuth redirect chain</returns>
        Task<string> SignIn(string provider, string? redirectUrl = null, LoginOptions? loginOptions = null);

        /// <summary>
        /// Update an existing logged in user by enabling them to also sign in using OAuth.
        /// </summary>
        /// <para>
        /// A successful authentication will result in a callback to the URL defined here or in the current project settings.
        /// </para>
        /// <param name="provider">Which provider to authenticate against</param>
        /// <param name="refreshJwt">A valid refresh JWT</param>
        /// <param name="redirectUrl">Optional redirect URL. If null, the default redirect URL in Descope console will be used.</param>
        /// <param name="allowAllMerge">Allow updating the existing user also if there are no common identifiers between the OAuth data and the existing user (like email)</param>
        /// <param name="loginOptions">Additional behaviors to perform during authentication</param>
        /// <returns>A URL that starts the OAuth redirect chain</returns>
        Task<string> UpdateUser(string provider, string refreshJwt, string? redirectUrl = null, bool allowAllMerge = false, LoginOptions? loginOptions = null);

        /// <summary>
        /// Completes an OAuth redirect chain.
        /// <para>
        /// This function exchanges the code received in the <c>code</c> URL parameter for an <c>AuthenticationResponse</c>.
        /// </para>
        /// <para>
        /// <b>Important:</b> The redirect URL might not contain a <c>code</c> URL parameter
        /// but can contain an <c>err</c> URL parameter instead. This can occur when attempting to
        /// sign up with an existing user or trying to sign in with a non-existing user.
        /// </para>
        /// </summary>
        /// <param name="code">The code extract from the code received in the <c>code</c> URL parameter</param>
        /// <returns>An <c>AuthenticationResponse</c> value upon successful exchange.</returns>
        Task<AuthenticationResponse> Exchange(string code);
    }

    /// <summary>
    /// Authenticate a user using an Enchanted Link.
    /// </summary>
    public interface IEnchantedLink
    {
        /// <summary>
        /// Authenticate a user using an enchanted link.
        /// <para>
        /// A successful authentication will result in a callback to the URL defined here or in the current project settings.
        /// </para>
        /// </summary>
        /// <param name="loginId">The login ID of the user</param>
        /// <param name="uri">Optional custom URI to redirect the user to after authentication, if empty then the URI set for the project in the descope console will be used instead</param>
        /// <param name="loginOptions">Additional optional behaviors to perform during authentication</param>
        /// <param name="refreshJwt">An optional valid refresh JWT, only needed if certain custom loginOptions were selected</param>
        /// <returns>An <c>EnchantedLinkResponse</c> value upon successful exchange.</returns>
        Task<EnchantedLinkResponse> SignIn(string loginId, string? uri, LoginOptions? loginOptions = null, string? refreshJwt = null);

        /// <summary>
        /// Start a sign up process using an enchanted link (need to perform polling afterwards to see if the user clicked the link).
        /// </summary>
        /// <param name="loginId">The login ID of the user</param>
        /// <param name="uri">Optional custom URI to redirect the user to after signup, if empty then the URI set for the project in the descope console will be used instead</param>
        /// <param name="signUpDetails">Additional optional user details</param>
        /// <param name="signUpOptions">Additional optional signup templates and custom claims</param>
        Task<EnchantedLinkResponse> SignUp(string loginId, string? uri, SignUpDetails? signUpDetails = null, SignUpOptions? signUpOptions = null);

        /// <summary>
        /// Start a sign up process or authenticate a user using an enchanted link (need to perform polling afterwards to see if the user clicked the link).
        /// </summary>
        /// <param name="loginId">The login ID of the user</param>
        /// <param name="uri">Optional custom URI to redirect the user to after signup, if empty then the URI set for the project in the descope console will be used instead</param>
        /// <param name="signUpOptions">Additional optional signup templates and custom claims</param>
        Task<EnchantedLinkResponse> SignUpOrIn(string loginId, string? uri, SignUpOptions? signUpOptions = null);

        /// <summary>
        /// Retrieve the enchanted link session after the user clicked the link.
        /// </summary>
        /// <para>
        /// Should be polled after invoking the SignIn/SignUp/SignUpOrIn method.
        /// </para>
        /// <param name="pendingRef">The pending reference received in the enchanted link response</param>
        /// <exception cref="DescopeException">Thrown if session request fails, could happen during polling until the user clicks the link and the token is verified</exception>
        /// <exception cref="ArgumentException">Thrown if the pendingRef is null or empty.</exception>
        Task<Session> GetSession(string pendingRef);

        /// <summary>
        /// Verifies the enchanted link token received in the redirect URL "t=" parameter.
        /// </summary>
        /// <para>
        /// Should be called once the user clicks the enchanted link and is redirected to the redirect URL.
        /// GetSession will not return a session until the token is verified.
        /// </para>
        /// <param name="token">The token to verify.</param>
        /// <exception cref="ArgumentException">Thrown if the token is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
        Task Verify(string token);

        /// <summary>
        /// Update the email address of a user using an enchanted link.
        /// </summary>
        /// <para>
        /// Will send an email to the new address with an enchanted link to verify the change.
        /// Verify must be called with the updated token received in the redirect URL "t=" parameter after the user clicks the link to complete the process.
        /// </para>
        /// <param name="loginId">The login ID of the user</param>
        /// <param name="email">The new email address</param>
        /// <param name="uri">Optional custom URI to redirect the user to after clicking the link, will use project default if empty</param>
        /// <param name="updateOptions">Optional email update options</param>
        /// <param name="templateOptions">Optional email template options</param>
        /// <param name="refreshJwt">Required valid refresh JWT</param>
        Task<EnchantedLinkResponse> UpdateUserEmail(
            string loginId,
            string email,
            string? uri,
            UpdateOptions? updateOptions,
            Dictionary<string, string>? templateOptions,
            string refreshJwt);
    }

    /// <summary>
    /// Authenticate a user using a SSO.
    /// <para>
    /// Use the Descope console to configure your SSO details in order for this method to work properly.
    /// </para>
    /// </summary>
    public interface ISsoAuth
    {
        /// <summary>
        /// Initiate a login flow based on tenant configuration (SAML/OIDC).
        /// <para>
        /// After the redirect chain concludes, finalize the authentication passing the
        /// received code the <c>Exchange</c> function.
        /// </para>
        /// </summary>
        /// <param name="tenant">The tenant ID or name, or an email address belonging to a tenant domain</param>
        /// <param name="redirectUrl">An optional parameter to generate the SSO link. If not given, the project default will be used.</param>
        /// <param name="prompt">Relevant only in case tenant configured with AuthType OIDC</param>
        /// <param name="forceAuthn">Force authentication even if the user has an active session</param>
        /// <param name="loginOptions">Require additional behaviors when authenticating a user.</param>
        /// <returns>The redirect URL that starts the SSO redirect chain</returns>
        Task<string> Start(string tenant, string? redirectUrl = null, string? prompt = null, bool? forceAuthn = null, LoginOptions? loginOptions = null);

        /// <summary>
        /// Finalize SSO authentication by exchanging the received <c>code</c> with an <c>AuthenticationResponse</c>
        /// </summary>
        /// <param name="code"> The code appended to the returning URL via the <c>code</c> URL parameter.</param>
        /// <returns>An <c>AuthenticationResponse</c> value upon successful exchange.</returns>
        Task<AuthenticationResponse> Exchange(string code);
    }

    /// <summary>
    /// Provides various APIs for authenticating and authorizing users of a Descope project.
    /// </summary>
    public interface IAuthentication
    {
        /// <summary>
        /// Authenticate a user using OTP (one-time password).
        /// </summary>
        public IOtp Otp { get; }

        /// <summary>
        /// Authenticate a user using OAuth.
        /// </summary>
        public IOAuth OAuth { get; }

        /// <summary>
        /// Authenticate a user using an enchanted link.
        /// </summary>
        public IEnchantedLink EnchantedLink { get; }

        /// <summary>
        /// Authenticate a user using SSO.
        /// </summary>
        public ISsoAuth Sso { get; }

        /// <summary>
        /// Validate a session JWT.
        /// <para>
        /// Should be called before any private API call that requires authorization.
        /// </para>
        /// </summary>
        /// <param name="sessionJwt">The session JWT to validate</param>
        /// <returns>A valid session token if valid</returns>
        Task<Token> ValidateSession(string sessionJwt);

        /// <summary>
        /// Refresh an expired session with a given refresh JWT.
        /// <para>
        /// Should be called when a session has expired (failed validation) to renew it.
        /// </para>
        /// </summary>
        /// <param name="refreshJwt">A valid refresh JWT</param>
        /// <returns>A refreshed session token</returns>
        Task<Token> RefreshSession(string refreshJwt);

        /// <summary>
        /// Validate a session JWT. If the session has expired, it will automatically be
        /// renewed by using the provided refresh JWT.
        /// </summary>
        /// <param name="sessionJwt"></param>
        /// <param name="refreshJwt"></param>
        /// <returns>A valid, potentially refreshed, session token</returns>
        Task<Token> ValidateAndRefreshSession(string sessionJwt, string refreshJwt);

        /// <summary>
        /// Exchange an access key for a session token.
        /// </summary>
        /// <param name="accessKey">The accessKey cleartext to exchange</param>
        /// <param name="loginOptions">Optional login options for the exchange</param>
        /// <returns>A valid session token if successful</returns>
        Task<Token> ExchangeAccessKey(string accessKey, AccessKeyLoginOptions? loginOptions = null);

        /// <summary>
        /// Ensure a validated session token has been granted the specified permissions.
        /// </summary>
        /// <param name="token">A valid session token</param>
        /// <param name="permissions">A list of permission to check</param>
        /// <param name="tenant">Provide a tenant ID if the permission belongs to a specific tenant</param>
        /// <returns>True if the token has been granted the given permission</returns>
        bool ValidatePermissions(Token token, List<string> permissions, string? tenant = null);

        /// <summary>
        /// Retrieves the permissions from top level token's claims, or for the provided
        /// tenant's claim, that match the specified permissions list.
        /// </summary>
        /// <param name="token">A valid session token</param>
        /// <param name="permissions">A list of permission to check</param>
        /// <param name="tenant">Provide a tenant ID if the permission belongs to a specific tenant</param>
        /// <returns>A list of matched permissions</returns>
        List<string> GetMatchedPermissions(Token token, List<string> permissions, string? tenant = null);

        /// <summary>
        /// Ensure a validated session token has been granted the specified roles.
        /// </summary>
        /// <param name="token">A valid session token</param>
        /// <param name="roles">A list of roles to check</param>
        /// <param name="tenant">Provide a tenant ID if the roles belongs to a specific tenant</param>
        /// <returns>True if the token has been granted the given roles</returns>
        bool ValidateRoles(Token token, List<string> roles, string? tenant = null);

        /// <summary>
        /// Retrieves the roles from top level token's claims, or for the provided
        /// tenant's claim, that match the specified role list.
        /// </summary>
        /// <param name="token">A valid session token</param>
        /// <param name="roles">A list of roles to check</param>
        /// <param name="tenant">Provide a tenant ID if the roles belongs to a specific tenant</param>
        /// <returns>A list of matched roles</returns>
        List<string> GetMatchedRoles(Token token, List<string> roles, string? tenant = null);

        /// <summary>
        /// Adds a dedicated claim to the JWTs to indicate which tenant the user is currently authenticated to.
        /// </summary>
        /// <param name="tenant">The tenant the user is logged into</param>
        /// <param name="refreshJwt">A valid refresh JWT</param>
        /// <returns>An updated session with a new claim indicating which tenant the user is currently logged into</returns>
        Task<Session> SelectTenant(string tenant, string refreshJwt);

        /// <summary>
        /// Logs out from the current session.
        /// </summary>
        /// <param name="refreshJwt">A valid refresh JWT</param>
        Task LogOut(string refreshJwt);

        /// <summary>
        /// Logout from all active sessions for the request user.
        /// </summary>
        /// <param name="refreshJwt">A valid refresh JWT</param>
        Task LogOutAll(string refreshJwt);

        /// <summary>
        /// Retrieve the current session user details.
        /// </summary>
        /// <param name="refreshJwt">A valid refresh JWT</param>
        /// <returns>The current session user details</returns>
        Task<UserResponse> Me(string refreshJwt);
    }
}
