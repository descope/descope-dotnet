using Descope.Auth;

namespace Descope;

/// <summary>
/// Implementation of token-related operations including validation, refresh, and access key exchange.
/// </summary>
internal class TokenActions : ITokenActions
{
    private readonly JwtValidator _jwtValidator;
    private readonly Auth.V1.Auth.AuthRequestBuilder _authRequestBuilder;

    internal TokenActions(
        JwtValidator jwtValidator,
        Auth.V1.Auth.AuthRequestBuilder authRequestBuilder)
    {
        _jwtValidator = jwtValidator;
        _authRequestBuilder = authRequestBuilder;
    }

    /// <inheritdoc/>
    public async Task<Token> ValidateSessionAsync(string sessionJwt)
    {
        if (string.IsNullOrEmpty(sessionJwt))
        {
            throw new DescopeException("Session JWT cannot be empty");
        }

        return await _jwtValidator.ValidateToken(sessionJwt);
    }

    /// <inheritdoc/>
    public async Task<Token> RefreshSessionAsync(string refreshJwt)
    {
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Refresh JWT cannot be empty");
        }

        // Validate the refresh token locally first
        var refreshToken = await _jwtValidator.ValidateToken(refreshJwt);

        // Call the refresh API with the JWT in the authorization context
        var response = await _authRequestBuilder.Refresh.PostWithJwtAsync(
            new Auth.Models.Onetimev1.RefreshSessionRequest(),
            refreshJwt);

        if (response == null || string.IsNullOrEmpty(response.SessionJwt))
        {
            throw new DescopeException("Failed to refresh session");
        }

        // Parse and return the new session token
        var sessionToken = await _jwtValidator.ValidateToken(response.SessionJwt);
        sessionToken.RefreshExpiration = refreshToken.Expiration;
        return sessionToken;
    }

    /// <inheritdoc/>
    public async Task<Token> ValidateAndRefreshSession(string sessionJwt, string refreshJwt)
    {
        if (string.IsNullOrEmpty(sessionJwt) && string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Both sessionJwt and refreshJwt are empty");
        }

        // Try to validate the session JWT first
        if (!string.IsNullOrEmpty(sessionJwt))
        {
            try
            {
                return await ValidateSessionAsync(sessionJwt);
            }
            catch
            {
                // Session validation failed, fall through to refresh
            }
        }

        // If session validation failed or session JWT is empty, try to refresh
        if (string.IsNullOrEmpty(refreshJwt))
        {
            throw new DescopeException("Cannot refresh session with empty refresh JWT");
        }

        return await RefreshSessionAsync(refreshJwt);
    }

    /// <inheritdoc/>
    public async Task<Token> ExchangeAccessKey(string accessKey, Auth.Models.Onetimev1.AccessKeyLoginOptions? loginOptions = null)
    {
        if (string.IsNullOrEmpty(accessKey))
        {
            throw new DescopeException("Access key cannot be empty");
        }

        // Call the access key exchange API with the access key in the authorization context
        var response = await _authRequestBuilder.Accesskey.Exchange.PostWithJwtAsync(
            new Auth.Models.Onetimev1.ExchangeAccessKeyRequest
            {
                LoginOptions = loginOptions
            },
            accessKey);

        if (response == null || string.IsNullOrEmpty(response.SessionJwt))
        {
            throw new Exception("Failed to exchange access key");
        }

        return await _jwtValidator.ValidateToken(response.SessionJwt);

    }
}
