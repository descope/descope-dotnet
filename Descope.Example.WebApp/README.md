# Descope OIDC Demo Web Application

A minimal ASP.NET web application demonstrating Descope OIDC authentication integration.

## Prerequisites

- .NET 6.0, 8.0, 9.0, or 10.0 SDK installed
- A Descope project (get your Project ID from the [Descope Console](https://app.descope.com))

## Configuration

1. Copy or create `localExampleSettings.json` in this directory:

```json
{
  "Descope": {
    "ProjectId": "your-project-id",
  }
}
```

2. Replace `your-project-id` with your actual Descope Project ID.

3. (Optional) Add `FlowId` to specify a default authentication flow.

## Running the Demo

Run the application using any of the supported .NET frameworks:

```bash
# .NET 6.0
dotnet run --framework net6.0

# .NET 8.0
dotnet run --framework net8.0

# .NET 9.0
dotnet run --framework net9.0

# .NET 10.0
dotnet run --framework net10.0
```

The application will start on `http://localhost:5000` (or the next available port).

## How It Works

1. Navigate to the home page
2. Click "Login with Descope Flow"
3. Complete the Descope authentication flow
4. Upon success, you'll be redirected to a success page showing your user information
5. Click "Logout" to sign out

## Customization

The demo showcases several customization options:

- **OIDC Events**: Add custom logging or behavior during authentication flow
- **Cookie Settings**: Customize session cookie name, expiration, and security settings
- **Flow ID**: Direct users to specific Descope flows

See [DescopeOidcDemo.cs](DescopeOidcDemo.cs) for the full implementation with comments.
