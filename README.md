# Descope .NET SDK

A .NET SDK for integrating with the Descope authentication and user management platform.

## Installation

```bash
dotnet add package Descope
```

## Client Configuration

Configure the Descope client using `DescopeClientOptions`:

```csharp
var options = new DescopeClientOptions
{
    ProjectId = "your-project-id",           // Required
    ManagementKey = "your-management-key",   // Optional, for management APIs
    AuthManagementKey = "your-auth-key",     // Optional, for accessing disabled auth APIs
    BaseUrl = "https://api.descope.com",     // Optional, auto-detected from project ID
    FgaCacheUrl = "https://fga.example.com", // Optional, if using the Descope FGA Cache Docker Container
    IsUnsafe = false                         // Optional, for dev/test only
};
```

## Creating the Client

### Using Dependency Injection (Recommended)

```csharp
services.AddDescopeClient(new DescopeClientOptions
{
    ProjectId = "your-project-id",
    ManagementKey = "your-management-key"
});

// Inject IDescopeClient in your services
public class MyService
{
    private readonly IDescopeClient _client;
    
    public MyService(IDescopeClient client)
    {
        _client = client;
    }
}
```

### Using Factory (Instance-based)

```csharp
var client = DescopeManagementClientFactory.Create(new DescopeClientOptions
{
    ProjectId = "your-project-id",
    ManagementKey = "your-management-key"
});
```

## Usage Examples

### Authentication API Call

```csharp
// Verify a magic link token
var response = await client.Auth.V1.Magiclink.Verify.PostAsync(
    new VerifyMagicLinkRequest { Token = "magic-link-token" });
```

### Management API V1 Call

```csharp
// Create a test user
var user = await client.Mgmt.V1.User.Create.Test.PostAsync(
    new CreateUserRequest
    {
        Identifier = "user@example.com",
        Email = "user@example.com",
        VerifiedEmail = true
    });
```

### Management API V2 Call

```csharp
// Search users
var searchResponse = await client.Mgmt.V2.User.Search.PostAsync(
    new SearchUsersRequest { Limit = 10 });
```

### Management Flows

Management flows allow you to run server-side flows with custom input and receive dynamic output. Since the output can contain arbitrary data, the SDK uses Kiota's `UntypedNode` types to represent dynamic values.

```csharp
using Microsoft.Kiota.Abstractions.Serialization;

// Run a management flow with custom input
var request = new RunManagementFlowRequest
{
    FlowId = "my-management-flow",
    Options = new ManagementFlowOptions
    {
        Input = new ManagementFlowOptions_input
        {
            AdditionalData = new Dictionary<string, object>
            {
                { "email", "user@example.com" },
                { "customParam", "customValue" }
            }
        }
    }
};

var response = await client.Mgmt.V1.Flow.Run.PostAsync(request);

// Access string values directly from AdditionalData
var email = response.Output?.AdditionalData?["email"] as string;

// Access nested objects using UntypedNode types
if (response.Output?.AdditionalData?.TryGetValue("result", out var resultObj) == true)
{
    // Cast to UntypedObject for nested JSON objects
    var untypedObj = (UntypedObject)resultObj;
    var properties = untypedObj.GetValue();
    
    // Access string properties
    var greeting = ((UntypedString)properties["greeting"]).GetValue();
    
    // Access numeric properties
    var count = ((UntypedInteger)properties["count"]).GetValue();
    
    // Access boolean properties
    var enabled = ((UntypedBoolean)properties["enabled"]).GetValue();
}
```

**Note:** Kiota uses `UntypedNode` subtypes for dynamic data:
- `UntypedObject` - nested objects (use `GetValue()` to get `Dictionary<string, UntypedNode>`)
- `UntypedString` - string values
- `UntypedInteger` / `UntypedDecimal` - numeric values
- `UntypedBoolean` - boolean values
- `UntypedArray` - arrays (use `GetValue()` to get `IEnumerable<UntypedNode>`)

## Token Validation

The SDK provides three methods for working with session tokens:

### ValidateSessionAsync

Validates a session JWT **locally** using cached public keys. The public key is fetched from the server only once and then cached for subsequent validations.

```csharp
var token = await client.Auth.ValidateSessionAsync(sessionJwt);
// Returns Token with claims, subject, expiration, etc.
```

### RefreshSessionAsync

Refreshes an expired session using a refresh JWT. This method **makes a remote API call** to generate a new session token.

```csharp
var newToken = await client.Auth.RefreshSessionAsync(refreshJwt);
// Returns a new session Token with updated expiration
```

### ValidateAndRefreshSession

Attempts to validate the session JWT first (locally), and if that fails or the session is empty, falls back to refreshing using the refresh JWT (remote call).

```csharp
var token = await client.Auth.ValidateAndRefreshSession(sessionJwt, refreshJwt);
// Returns valid Token, using local validation when possible
```

**Performance Note:** Validation calls (`ValidateSessionAsync`, `ValidateAndRefreshSession`) are highly efficient as they use locally cached public keys. Only `RefreshSessionAsync` and the refresh fallback in `ValidateAndRefreshSession` make remote API calls.

## Authenticated User Operations

Some authentication operations require an authenticated user context and must be called with a refresh JWT. For these operations, use the `PostWithJwt` extension methods that explicitly require the refresh token.

### Example: Update User Email

```csharp
// Update user email using magic link (requires refresh JWT)
var response = await client.Auth.V1.Magiclink.Update.Email.PostWithJwtAsync(
    new UpdateUserEmailMagicLinkRequest
    {
        Email = "newemail@example.com",
        RedirectUrl = "https://myapp.com/verify"
    },
    refreshJwt);
```

Other operations requiring `PostWithJwt` include, among others: updating phone numbers, passwords, TOTP settings, WebAuthn devices, and getting user details via the `/me` endpoint.

## ASP.NET OIDC Integration

For ASP.NET Core applications, use the `AddDescopeOidcAuthentication` extension method to integrate Descope as your Identity Provider (IdP) using OpenID Connect (OIDC):

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddDescopeOidcAuthentication(options =>
    {
        options.ProjectId = "your-project-id";
    });
```

See the [OIDC Demo Application](Descope.Example.WebApp/README.md) for a complete working example with additional customization options.

## For Maintainers

If you're maintaining or contributing to this SDK, see the [Maintainer Guide](README-maintainer.md) for detailed information about code generation, extension methods, testing, and development workflows.