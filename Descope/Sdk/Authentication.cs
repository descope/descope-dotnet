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
        /// <param name="loginOptions">Require additional behaviors when authenticating a user.</param>
        /// <returns>The redirect URL that starts the SSO redirect chain</returns>
        Task<string> Start(string tenant, string? redirectUrl = null, string? prompt = null, LoginOptions? loginOptions = null);

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
        /// Authenticate a user using a SSO.
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
