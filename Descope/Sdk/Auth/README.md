# Local JWT Validation

The Descope .NET SDK now supports local JWT validation, similar to the Go SDK implementation. This allows you to validate session tokens without making HTTP requests to the Descope API, improving performance and reducing latency.

## Features

- **Local JWT Validation**: Validate session JWTs locally using cached public keys
- **Token Claims Access**: Easy access to all JWT claims including custom claims
- **Permission & Role Validation**: Built-in methods to validate permissions and roles
- **Tenant Support**: Validate permissions and roles for specific tenants

## Usage

### Basic Validation

```csharp
// Create the Descope client
var client = DescopeManagementClientFactory.Create(
    projectId: "your-project-id",
    managementKey: "your-management-key"
);

// Validate a session JWT locally (no HTTP call)
var token = await client.Auth.ValidateSessionLocal(sessionJwt);

// Access token information
Console.WriteLine($"User ID: {token.Subject}");
Console.WriteLine($"Project ID: {token.ProjectId}");
Console.WriteLine($"Expires: {token.Expiration}");
```

### Working with Claims

```csharp
// Get all claims
var claims = token.Claims;

// Get a specific claim
var email = token.GetClaimValue("email");

// Access custom claims
if (token.Claims.ContainsKey("nsec"))
{
    var customClaims = token.Claims["nsec"];
}
```

### Validating Permissions

```csharp
// Validate that the user has all specified permissions
var hasPermissions = token.ValidatePermissions(
    new List<string> { "read:users", "write:users" }
);

// Get matched permissions
var matchedPermissions = token.GetMatchedPermissions(
    new List<string> { "read:users", "write:users", "delete:users" }
);
```

### Validating Roles

```csharp
// Validate that the user has all specified roles
var hasRoles = token.ValidateRoles(
    new List<string> { "admin", "editor" }
);

// Get matched roles
var matchedRoles = token.GetMatchedRoles(
    new List<string> { "admin", "editor", "viewer" }
);
```

### Tenant-Specific Validation

```csharp
// Get all tenants associated with the token
var tenants = token.GetTenants();

// Validate permissions for a specific tenant
var hasPermissions = token.ValidatePermissions(
    new List<string> { "read:data" },
    tenant: "tenant-123"
);

// Validate roles for a specific tenant
var hasRoles = token.ValidateRoles(
    new List<string> { "admin" },
    tenant: "tenant-123"
);

// Get a value from a tenant's claims
var tenantValue = token.GetTenantValue("tenant-123", "customField");
```

## How It Works

1. **Public Key Caching**: On first validation, the SDK fetches the project's public keys from Descope and caches them in memory
2. **Local Validation**: Subsequent validations use the cached keys to verify JWT signatures locally
3. **No HTTP Calls**: After the initial key fetch, validation is performed entirely locally without any HTTP requests

## Performance Benefits

- **Reduced Latency**: No network round-trip for validation
- **Lower API Usage**: Fewer calls to Descope API endpoints
- **Better Scalability**: Validation can handle higher throughput
- **Offline Capability**: Can validate tokens without network connectivity (after initial key fetch)

## Comparison with HTTP Validation

```csharp
// ❌ HTTP-based validation (makes an API call every time)
var response = await client.Auth.V1.Validate.PostAsync(
    new ValidateSessionRequest { /* ... */ }
);

// ✅ Local validation (no API call, uses cached public keys)
var token = await client.Auth.ValidateSessionLocal(sessionJwt);
```

## When to Use

**Use Local Validation when:**
- You need high-performance token validation
- You're validating tokens frequently
- You want to reduce API calls and latency
- You need to check permissions/roles from the token

**Use HTTP Validation when:**
- You need to verify the token hasn't been revoked server-side
- You want the most up-to-date validation logic
- You're not concerned about the additional latency

## Error Handling

```csharp
try
{
    var token = await client.Auth.ValidateSessionLocal(sessionJwt);
    // Token is valid
}
catch (InvalidOperationException ex)
{
    // Local validation not available (missing configuration)
    Console.WriteLine($"Configuration error: {ex.Message}");
}
catch (ArgumentException ex)
{
    // Invalid input (e.g., empty JWT)
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (Exception ex)
{
    // Token validation failed
    Console.WriteLine($"Token is invalid: {ex.Message}");
}
```

## Checking Availability

```csharp
// Check if local validation is available
if (client.Auth.CanValidateLocally)
{
    var token = await client.Auth.ValidateSessionLocal(sessionJwt);
}
else
{
    // Fall back to HTTP validation
    var response = await client.Auth.V1.Validate.PostAsync(/* ... */);
}
```
