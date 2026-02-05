using Microsoft.IdentityModel.JsonWebTokens;
using System.Text.Json;

namespace Descope;

/// <summary>
/// Represents a validated JWT token with convenient access to claims.
/// </summary>
public class Token
{
    private readonly JsonWebToken _jwt;

    /// <summary>
    /// Gets the raw JWT string.
    /// </summary>
    public string Jwt => _jwt.EncodedToken;

    /// <summary>
    /// Gets the token's expiration time.
    /// </summary>
    public DateTime Expiration => _jwt.ValidTo;

    /// <summary>
    /// Gets or sets the refresh token's expiration time.
    /// </summary>
    public DateTime? RefreshExpiration { get; set; }

    /// <summary>
    /// Gets the project ID from the token.
    /// </summary>
    public string ProjectId => GetClaimValue("iss") ?? string.Empty;

    /// <summary>
    /// Gets the subject (user ID) from the token.
    /// </summary>
    public string Subject => GetClaimValue("sub") ?? string.Empty;

    // alias for Subject
    public string Id => Subject;

    /// <summary>
    /// Gets the current tenant ID (Descope Current Tenant) from the token
    /// </summary>
    public string? CurrentTenant => GetClaimValue("dct");

    /// <summary>
    /// Gets all claims from the token.
    /// </summary>
    public Dictionary<string, object> Claims
    {
        get
        {
            var claims = new Dictionary<string, object>();
            foreach (var claim in _jwt.Claims)
            {
                if (claim.Value != null)
                {
                    claims[claim.Type] = claim.Value;
                }
            }
            return claims;
        }
    }

    /// <summary>
    /// Initializes a new instance of the Token class.
    /// </summary>
    /// <param name="jwt">The JSON Web Token.</param>
    public Token(JsonWebToken jwt)
    {
        _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
    }

    /// <summary>
    /// Gets a claim value as a string.
    /// </summary>
    /// <param name="claimType">The claim type.</param>
    /// <returns>The claim value or null if not found.</returns>
    public string? GetClaimValue(string claimType)
    {
        return _jwt.GetClaim(claimType)?.Value;
    }

    /// <summary>
    /// Gets the list of tenants associated with this token.
    /// </summary>
    /// <returns>A list of tenant IDs.</returns>
    public List<string> GetTenants()
    {
        var tenantsClaim = _jwt.GetClaim("tenants");
        if (tenantsClaim?.Value == null) return new List<string>();

        try
        {
            var tenantsData = JsonSerializer.Deserialize<Dictionary<string, object>>(tenantsClaim.Value);
            return tenantsData?.Keys.ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets a specific value from a tenant's claims.
    /// </summary>
    /// <param name="tenant">The tenant ID.</param>
    /// <param name="key">The claim key.</param>
    /// <returns>The claim value or null if not found.</returns>
    public object? GetTenantValue(string tenant, string key)
    {
        var tenantsClaim = _jwt.GetClaim("tenants");
        if (tenantsClaim?.Value == null) return null;

        try
        {
            var tenantsData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(tenantsClaim.Value);
            if (tenantsData != null && tenantsData.TryGetValue(tenant, out var tenantData))
            {
                if (tenantData.TryGetValue(key, out var value))
                {
                    return value;
                }
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Validates that the token has all the specified permissions.
    /// </summary>
    /// <param name="permissions">The list of permissions to validate.</param>
    /// <param name="tenant">Optional tenant ID to check permissions for a specific tenant.</param>
    /// <returns>True if all permissions are present, false otherwise.</returns>
    public bool ValidatePermissions(List<string> permissions, string? tenant = null)
    {
        if (tenant != null && tenant.Length > 0 && !GetTenants().Contains(tenant))
        {
            return false;
        }

        var claimItems = GetAuthorizationClaimItems("permissions", tenant);
        return permissions.All(p => claimItems.Contains(p));
    }

    /// <summary>
    /// Gets the permissions from the token that match the provided list.
    /// </summary>
    /// <param name="permissions">The list of permissions to check.</param>
    /// <param name="tenant">Optional tenant ID to check permissions for a specific tenant.</param>
    /// <returns>The list of matched permissions.</returns>
    public List<string> GetMatchedPermissions(List<string> permissions, string? tenant = null)
    {
        if (tenant != null && tenant.Length > 0 && !GetTenants().Contains(tenant))
        {
            return new List<string>();
        }

        var claimItems = GetAuthorizationClaimItems("permissions", tenant);
        return permissions.Where(p => claimItems.Contains(p)).ToList();
    }

    /// <summary>
    /// Validates that the token has all the specified roles.
    /// </summary>
    /// <param name="roles">The list of roles to validate.</param>
    /// <param name="tenant">Optional tenant ID to check roles for a specific tenant.</param>
    /// <returns>True if all roles are present, false otherwise.</returns>
    public bool ValidateRoles(List<string> roles, string? tenant = null)
    {
        if (tenant != null && tenant.Length > 0 && !GetTenants().Contains(tenant))
        {
            return false;
        }

        var claimItems = GetAuthorizationClaimItems("roles", tenant);
        return roles.All(r => claimItems.Contains(r));
    }

    /// <summary>
    /// Gets the roles from the token that match the provided list.
    /// </summary>
    /// <param name="roles">The list of roles to check.</param>
    /// <param name="tenant">Optional tenant ID to check roles for a specific tenant.</param>
    /// <returns>The list of matched roles.</returns>
    public List<string> GetMatchedRoles(List<string> roles, string? tenant = null)
    {
        if (tenant != null && tenant.Length > 0 && !GetTenants().Contains(tenant))
        {
            return new List<string>();
        }

        var claimItems = GetAuthorizationClaimItems("roles", tenant);
        return roles.Where(r => claimItems.Contains(r)).ToList();
    }

    private List<string> GetAuthorizationClaimItems(string claim, string? tenant)
    {
        if (tenant == null || tenant.Length == 0)
        {
            // Collect all claims with the matching type.
            // JsonWebToken expands JSON array claims (e.g. "roles": ["a", "b"])
            // into multiple individual Claim objects with the same type,
            // so we must enumerate all of them rather than using GetClaim()
            // which only returns the first match.
            var claimValues = _jwt.Claims
                .Where(c => c.Type == claim)
                .Select(c => c.Value)
                .ToList();
            if (claimValues.Count > 0) return claimValues;
        }
        else
        {
            var tenantValue = GetTenantValue(tenant, claim);
            if (tenantValue is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    try
                    {
                        var list = jsonElement.Deserialize<List<string>>();
                        if (list != null) return list;
                    }
                    catch { }
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return new List<string> { jsonElement.GetString()! };
                }
            }
        }

        return new List<string>();
    }
}
