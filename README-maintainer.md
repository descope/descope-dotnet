# Descope .NET SDK - Maintainer Guide

This guide is intended for maintainers of the Descope .NET SDK. For SDK usage documentation, see the main [README.md](README.md).

## Table of Contents

- [Getting Started](#getting-started)
- [Makefile Usage](#makefile-usage)
- [Kiota Code Generation](#kiota-code-generation)
- [Extension Methods](#extension-methods)
- [Middleware Overview](#middleware-overview)
- [Testing](#testing)
- [Releasing Versions](#releasing-versions)

## Getting Started

### Prerequisites

- .NET SDK 6.0, 8.0, 9.0, and 10.0 (for running all tests)
- Kiota CLI tool (automatically installed by `make check-kiota`)
- Access to the `GODESCOPE` environment variable pointing to the Descope root Go folder

### Quick Start

The most common workflow for maintainers:

```bash
# Regenerate Kiota files and build the SDK
make

# Run quick tests (net8.0 framework only)
make test-quick
```

## Makefile Usage

The project uses a Makefile to automate common tasks. Here are the available targets:

### Primary Targets

- **`make` or `make build`** (default): Regenerates all Kiota client files and rebuilds the C# project
- **`make test`**: Runs unit tests across all target frameworks (net6.0, net8.0, net9.0, net10.0)
- **`make test-quick`**: Runs unit tests for net8.0 only (faster for development)
- **`make generate`**: Regenerates all Kiota client files without building

### Individual Generation Targets

- **`make generate-mgmt`**: Regenerates Management API Kiota client files only
- **`make generate-auth`**: Regenerates Auth API Kiota client files only
- **`make check-kiota`**: Checks if Kiota is installed, installs if missing

### Utility Targets

- **`make cover`**: Runs tests with coverage report (requires ReportGenerator)
- **`make clean`**: Cleans build artifacts
- **`make post-process-obsolete`**: Applies `[Obsolete]` annotations from `Obsolete.csv` (called during `make generate`)
- **`make help`**: Shows all available targets with descriptions

## Kiota Code Generation

The SDK uses [Microsoft Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview) to auto-generate API client code from OpenAPI specifications.

### Generated Code Structure

Generated code is placed in:
- **Management API**: `Descope/Generated/Mgmt/`
- **Auth API**: `Descope/Generated/Auth/`

### Excluded Endpoints

Not all endpoints from the OpenAPI specs are included in the SDK. Endpoints are excluded in the Makefile using the `--exclude-path` option for Kiota.

### Post-Processing

After Kiota generation, the `post-process-obsolete` target applies `[Obsolete]` attributes to methods that have better alternatives (see [Extension Methods](#extension-methods) below).

## Extension Methods

To provide a better developer experience, the SDK includes extension methods that wrap Kiota-generated methods with cleaner, more intuitive APIs.

### Why Extension Methods?

Kiota-generated methods often require complex configuration objects or have implicit requirements (like JWT tokens) that aren't clear from the method signature. Extension methods make these requirements explicit and provide sensible defaults.

### Two Extension Classes

#### 1. `AuthExtensions.cs` - Authentication Operations

Located at `Descope/Sdk/Auth/AuthExtensions.cs`, this class provides extensions for authentication operations that require JWT tokens.

**Key patterns:**
- **`WithJwt` methods**: Operations requiring a refresh JWT (updates, logout, etc.)
- **`WithKey` methods**: Operations requiring an access key (key exchange)
- **Query parameter helpers**: Simplifying SSO authorize flows

**Example:**

```csharp
// Instead of manually configuring request with JWT:
await client.Auth.V1.Magiclink.Update.Email.PostAsync(request, config => {
    config.Headers.Add("Authorization", $"Bearer {refreshJwt}");
});

// Use the extension method:
await client.Auth.V1.Magiclink.Update.Email.PostWithJwtAsync(request, refreshJwt);
```

#### 2. `MgmtExtensions.cs` - Management Operations

Located at `Descope/Sdk/Mgmt/MgmtExtensions.cs`, this class provides extensions for management operations with clearer parameter naming.

**Key patterns:**
- **`WithId` methods**: Loading entities by ID
- **`WithTenantId` methods**: Tenant-scoped operations
- **`WithIdentifier` methods**: Flexible user identifier lookups (userID or loginID)
- **`WithSettingsResponse` methods**: Update operations using response objects

**Example:**

```csharp
// Instead of:
await client.Mgmt.V1.Tenant.GetAsync(config => {
    config.QueryParameters.Id = tenantId;
});

// Use:
await client.Mgmt.V1.Tenant.GetWithIdAsync(tenantId);
```
### Marking Methods as Obsolete

When an extension method provides a better API than the Kiota-generated method, we mark the generated method as obsolete to guide developers toward the better approach.

#### The `Obsolete.csv` File

This CSV file (located at project root) defines which methods should be marked with `[Obsolete]` attributes:

```csv
RelativeFilePath,Method,Replacement
Descope/Generated/Mgmt/V1/Mgmt/User/UserRequestBuilder.cs,GetAsync,GetWithIdentifierAsync
Descope/Generated/Auth/V1/Auth/Magiclink/Update/Email/EmailRequestBuilder.cs,PostAsync,PostWithJwtAsync
```

**Columns:**
- `RelativeFilePath`: Path to the generated file (relative to project root)
- `Method`: The Kiota-generated method name to mark obsolete
- `Replacement`: The extension method that should be used instead

**Important:** The `post-process-obsolete` target runs automatically as part of `make generate`, so you don't need to manually apply these annotations.

### Best Practices for Extension Methods

1. **Explicit parameters**: Make implicit requirements (like JWTs) explicit in the method signature
2. **Validation**: Validate parameters and throw `DescopeException` with clear messages
3. **Documentation**: Include XML comments with usage examples
4. **Consistency**: Follow existing naming patterns (`WithJwt`, `WithId`, etc.)
5. **Update Obsolete.csv**: ALWAYS add an entry when wrapping a generated method

## Middleware Overview

The SDK uses custom HTTP middleware handlers to address OpenAPI inconsistencies and routing requirements.

### 1. FixRootResponseBodyHandler

**Purpose:** Corrects OpenAPI spec inconsistencies for endpoints using the protobuf `response_body` option.

**Problem:** Some endpoints return "flat" fields at the root level instead of nested under a specific field name, which doesn't match the generated client's expectations.

**Location:** `Descope/Sdk/Internal/Middleware/FixRootResponseBodyHandler.cs`

### 2. FgaCacheUrlHandler

**Purpose:** Routes specific FGA (Fine-Grained Authorization) operations to an alternate cache URL when configured.

**Problem:** FGA cache operations need to hit a different endpoint than the main API for performance reasons.

**Location:** `Descope/Sdk/Internal/Middleware/FgaCacheUrlHandler.cs`

**Affected endpoints (POST only):**
- `/v1/mgmt/fga/schema` - SaveSchema
- `/v1/mgmt/fga/relations` - CreateRelations
- `/v1/mgmt/fga/relations/delete` - DeleteRelations
- `/v1/mgmt/fga/check` - Check

**Configuration:**

```csharp
var client = new DescopeClient(new DescopeClientOptions
{
    ProjectId = "your-project-id",
    FgaCacheUrl = "https://fga-cache.descope.com" // Optional
});
```

### Adding New Middleware

If you need to add new middleware:

1. Create a class inheriting from `DelegatingHandler` in `Descope/Sdk/Internal/Middleware/`
2. Override `SendAsync` to implement your logic
3. Register it in both `DescopeClientFactory.cs` and `DescopeServiceCollectionExtensions.cs`:
4. Document it in this README

## Testing

### Test Structure

Tests are located in `Descope.Test/`:
- **UnitTests/**: Fast, isolated tests using mocks
- **IntegrationTests/**: Tests against real Descope APIs (require configuration)
- **Helpers/**: Test utilities and mocks

### Running Tests

```bash
# All frameworks (net6.0, net8.0, net9.0, net10.0)
make test

# Quick run (net8.0 only) - recommended during development
make test-quick

# With coverage report
make cover
```

### Integration Tests

Integration tests require a valid Descope project. To run locally, create and populate the git-ignored `Descope.Test/appsettingsTest.json` file like in the example below.

```json
{
  "AppSettings": {
    "ProjectId": "P**************a",
    "ManagementKey": "K********************************",
    "BaseURL": "http://localhost:8000",
    "Unsafe": "true"
  }
}
```

**Important:** In the CI environment, integration tests use environment variables instead of `appsettingsTest.json`.
**Important:** Integration tests are rate-limited. See `Descope.Test/IntegrationTests/RATE_LIMITING.md` for details.

## Releasing Versions

The SDK maintains two separate version lines with different release processes:

### 1.x.x - Kiota-Based SDK (Current)

The current Kiota-generated SDK uses automated release management via [release-please](https://github.com/googleapis/release-please).

**How it works:**
- Release-please automatically maintains a release PR that tracks all changes merged into `main`
- The PR is continuously updated with new changes and follows [Conventional Commits](https://www.conventionalcommits.org/) to determine version bumps
- Changelog entries are automatically generated from commit messages

**To release a new version:**
1. Review the open release-please PR to verify the changes and version bump
2. Merge the release-please PR into `main`
3. Release-please will automatically:
   - Create a GitHub release
   - Publish the package to NuGet
   - Update the version number

**Important:** Make sure your commit messages follow conventional commit format (e.g., `feat:`, `fix:`, `chore:`) for proper changelog generation and version bumping.

### 0.x.x - Legacy SDK (Maintenance Only)

The legacy manually-generated SDK is maintained on the `main-v0` branch for critical bug fixes and vulnerability updates only.

**When to use:**
- Security vulnerabilities affecting `0.x.x` users
- Critical bug fixes that can't wait for users to migrate to `1.x.x`

**Release process:**
1. **Create and merge PR to `main-v0`:**
   - The `main-v0` branch is protected and tracks the `0.x.x` SDK version
   - Create your fix/update branch from `main-v0`
   - Open a PR targeting `main-v0` (NOT `main`)
   - Get it reviewed and merged

2. **Manually create a release:**
   - Go to the [GitHub Releases page](https://github.com/descope/descope-dotnet/releases)
   - Click "Draft a new release"
   - **Important:** Select `main-v0` as the target branch (NOT `main`)
   - Set the tag version with `0.` prefix (e.g., `0.8.1`, `0.8.2`)
   - Fill in release notes describing the fixes
   - Publish the release

**Important Notes:**
- Release-please does NOT track the `main-v0` branch - releases must be created manually
- Always ensure the version number starts with `0.` to distinguish it from the `1.x.x` line
- NuGet package publication is handled automatically by GitHub Actions on release creation