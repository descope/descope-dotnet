namespace Descope;

/// <summary>
/// Interface for token-related operations including validation, refresh, and access key exchange.
/// </summary>
public interface ITokenActions
{
    /// <summary>
    /// Validates a session JWT locally using cached keys, loading keys if needed.
    /// </summary>
    /// <param name="sessionJwt">The session JWT to validate.</param>
    /// <returns>A validated Token object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when JWT validator is not initialized.</exception>
    /// <exception cref="Exception">Thrown when the session JWT is invalid.</exception>
    Task<Token> ValidateSessionAsync(string sessionJwt);

    /// <summary>
    /// Refreshes a session using a refresh JWT. Validates the refresh JWT locally,
    /// calls the refresh API, and returns a new session token.
    /// </summary>
    /// <param name="refreshJwt">The refresh JWT to use for refreshing the session.</param>
    /// <returns>A new Token object with updated session information.</returns>
    /// <exception cref="ArgumentException">Thrown when refreshJwt is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when JWT validator is not initialized.</exception>
    /// <exception cref="Exception">Thrown when the refresh JWT is invalid or refresh fails.</exception>
    Task<Token> RefreshSessionAsync(string refreshJwt);

    /// <summary>
    /// Validates and refreshes a session. First attempts to validate the session JWT,
    /// and if that fails or is empty, attempts to refresh using the refresh JWT.
    /// </summary>
    /// <param name="sessionJwt">The session JWT to validate.</param>
    /// <param name="refreshJwt">The refresh JWT to use if session validation fails.</param>
    /// <returns>A validated or refreshed Token object.</returns>
    /// <exception cref="ArgumentException">Thrown when both JWTs are empty.</exception>
    /// <exception cref="Exception">Thrown when both validation and refresh fail.</exception>
    Task<Token> ValidateAndRefreshSession(string sessionJwt, string refreshJwt);

    /// <summary>
    /// Exchanges an access key for a session JWT.
    /// </summary>
    /// <param name="accessKey">The access key to exchange.</param>
    /// <param name="loginOptions">Optional login options for the exchange.</param>
    /// <returns>A Token object representing the exchanged session.</returns>
    /// <exception cref="ArgumentException">Thrown when accessKey is empty.</exception>
    /// <exception cref="Exception">Thrown when the exchange fails or token parsing fails.</exception>
    Task<Token> ExchangeAccessKey(string accessKey, Auth.Models.Onetimev1.AccessKeyLoginOptions? loginOptions = null);

    /// <summary>
    /// Validates a DPoP (Demonstrated Proof of Possession) proof JWT against a session token (RFC 9449).
    /// If the session token does not have a <c>cnf.jkt</c> claim it is not DPoP-bound and this method
    /// returns immediately without error.
    /// </summary>
    /// <param name="sessionToken">The validated session JWT string.</param>
    /// <param name="dpopProof">The DPoP proof JWT from the <c>DPoP</c> HTTP header.</param>
    /// <param name="method">The HTTP method of the request (e.g. "GET", "POST").</param>
    /// <param name="requestUrl">The full request URL.</param>
    /// <exception cref="DescopeException">Thrown when the DPoP proof is missing or invalid.</exception>
    void ValidateDPoP(string sessionToken, string dpopProof, string method, string requestUrl);

}
