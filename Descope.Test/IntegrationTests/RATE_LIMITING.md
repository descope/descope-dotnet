# Integration Test Rate Limiting

## Problem
The integration tests were hitting Descope API rate limits in CI:
- General API: 100 requests per 60 seconds
- Backend SDK: 1000 requests per 10 seconds  
- User management (`/v1/mgmt/user/*`): 500 requests per 60 seconds
- User create/batch: 100 requests per 60 seconds
- User update/search: 200 requests per 60 seconds

## Solution
Integration tests now use **partitioned test collections with trait-based filtering** to enable controlled parallel execution while respecting rate limits:

1. **Per-test delays** - Each test waits 1000ms after the previous test completes (0ms on macOS for faster local development)
2. **Partitioned collections** - Tests are organized into 5 independent collections that can run in parallel
3. **Sequential within collections** - Tests within the same collection run one at a time
4. **Thread-safe** - Semaphore ensures proper synchronization
5. **Parallel execution** - Up to 4 collections can run simultaneously
6. **Trait-based filtering** - Tests are marked with `[Trait("Category", "...")]` attributes for flexible filtering in CI

### Test Collections
Tests are partitioned into 5 collections based on resource conflicts. Each test class has both a `[Collection]` attribute for XUnit organization and a `[Trait("Category", "...")]` attribute for CI filtering:

1. **Authentication Tests** (`[Trait("Category", "Authentication")]`)
   - Auth flow tests (OTP, MagicLink, Password, etc.)
   - Stateless operations that don't modify shared resources
   - Also includes DI configuration tests (e.g., `ServiceDITests`)
   - Classes: `AuthenticationTests`, `MagicLinkTests`, `OtpEmailTests`, `OtpSmsTests`, `PasswordAuthenticationTests`, `ServiceDITests` (dependency injection)

2. **User Management Tests** (`[Trait("Category", "UserManagement")]`)
   - User CRUD operations
   - Sequential execution to manage rate limits (100 creates/60s, 200 updates/60s)
   - Classes: `UserTests`

3. **Tenant Management Tests** (`[Trait("Category", "TenantManagement")]`)
   - Tenant operations
   - Sequential execution to avoid conflicts when listing/verifying tenants
   - Classes: `TenantTests`

4. **Authorization Tests** (`[Trait("Category", "Authorization")]`)
   - Sequential execution to avoid conflicts with shared authorization resources
   - Classes: `RoleTests`, `PermissionTests`, `FgaTests`

5. **Project & Settings Tests** (`[Trait("Category", "ProjectSettings")]`)
   - Sequential execution as these modify project-level settings
   - Classes: `ProjectTests`, `SsoTests`, `SsoApplicationTests`, `PasswordSettingsTests`, `ThirdPartyApplicationTests`, `JwtTests`, `AccessKeyTests`

### How It Works
XUnit creates a **new instance of the test class for each test method**. This means:
- Constructor is called before each test → enforces delay
- Test method runs
- Dispose() is called after each test → updates timing

**Platform-specific behavior:**
- **macOS**: 0ms delay (fast local development)
- **Linux/CI**: 1000ms delay (respects API rate limits)

With ~97 integration tests distributed across 5 collections running in parallel (max 4 threads), integration tests will take significantly less time than sequential execution. On macOS, tests run without delays. Unit tests run without delays on all platforms.

### Implementation
- **`RateLimitedIntegrationTest`** (base class in `RateLimitTestFixture.cs`):
  - All integration test classes inherit from this
  - Constructor enforces platform-specific delay from last test (0ms on macOS, 1000ms elsewhere)
  - Dispose() updates the last test completion time
  - Uses a static semaphore for thread safety
  - Detects platform using `OperatingSystem.IsMacOS()`

- **`IntegrationTestCollection.cs`**:
  - Defines 5 partitioned test collections
  - Each collection groups tests that don't conflict with each other
  - Collections can run in parallel; tests within a collection run sequentially
  
- **All 17 integration test classes**:
  - Inherit from `RateLimitedIntegrationTest`
  - Marked with appropriate `[Collection("...")]` attribute for XUnit organization
  - Marked with `[Trait("Category", "...")]` attribute for CI filtering

### Configuration
The delay is dynamically set based on the platform in `RateLimitedIntegrationTest`:
```csharp
private static int GetDelayBasedOnPlatform()
{
    return OperatingSystem.IsMacOS() ? 0 : 1000;
}
```

To override the default behavior:
- **For faster CI builds** (risk hitting rate limits): Change `1000` to a lower value
- **For more conservative rate limiting**: Change `1000` to a higher value (e.g., 1500ms for ~40 tests/min)
- **To enable delays on macOS**: Modify the logic to check an environment variable instead

### XUnit Runner Settings
The `xunit.runner.json` file enables controlled parallelization:
```json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}
```

This allows up to 4 test collections to run in parallel while ensuring tests within a collection run sequentially.

### CI Configuration
The CI workflow (`.github/workflows/ci.yaml`) uses a matrix strategy to run tests from each category in parallel:

```yaml
strategy:
  fail-fast: false
  matrix:
    category:
      - Authentication
      - UserManagement
      - TenantManagement
      - Authorization
      - ProjectSettings
```

Each matrix job filters tests using the `Category` trait:
```bash
dotnet test --filter "Category=${{ matrix.category }}"
```

This approach:
- Runs 5 parallel CI jobs (one per category)
- Each job runs only the tests in its category
- Tests within a category still run sequentially (via XUnit collections)
- Provides better CI reporting with separate jobs per category

## Expected Behavior
With this implementation:

**On macOS (local development):**
- **No delays** between integration tests
- Tests run across multiple collections in parallel
- Tests complete quickly (~15-20 seconds for integration tests)
- Fast feedback loop for developers

**In CI/Linux environments:**
- **5 collections running in parallel** (max 4 threads)
- Largest collection has ~20-30 tests with 1000ms delays = ~20-30 seconds + execution time
- **Estimated total time: ~45-90 seconds** (vs. ~2-3 minutes sequential)
- **2-4x speedup** compared to sequential execution
- Unit tests (86 tests) run quickly without delays

## Performance Impact
- **Before**: All 97 integration tests ran sequentially → ~97 seconds of delays + execution time = 2-3 minutes
- **After**: Tests partitioned into 5 collections, running up to 4 in parallel → ~20-30 seconds of delays (longest collection) + execution time = 45-90 seconds
- **Speedup**: 2-4x faster integration test execution in CI