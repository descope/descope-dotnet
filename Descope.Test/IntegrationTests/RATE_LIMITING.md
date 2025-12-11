# Integration Test Rate Limiting

## Problem
The integration tests were hitting Descope API rate limits in CI:
- General API: 100 requests per 60 seconds
- Backend SDK: 1000 requests per 10 seconds  
- User management (`/v1/mgmt/user/*`): 500 requests per 60 seconds
- User create/batch: 100 requests per 60 seconds
- User update/search: 200 requests per 60 seconds

## Solution
Integration tests now enforce rate limiting through a base class pattern:
1. **Per-test delays** - Each test waits 1000ms after the previous test completes (0ms on macOS for faster local development)
2. **Sequential execution** - Tests in the "Integration Tests" collection run one at a time
3. **Thread-safe** - Semaphore ensures proper synchronization

### How It Works
XUnit creates a **new instance of the test class for each test method**. This means:
- Constructor is called before each test → enforces delay
- Test method runs
- Dispose() is called after each test → updates timing

**Platform-specific behavior:**
- **macOS**: 0ms delay (fast local development)
- **Linux/CI**: 1000ms delay (respects API rate limits)

With ~97 integration tests and 1000ms delays in CI, integration tests will take at least 97 seconds (plus test execution time). On macOS, tests run without delays. Unit tests run without delays on all platforms.

### Implementation
- **`RateLimitedIntegrationTest`** (base class in `RateLimitTestFixture.cs`):
  - All integration test classes inherit from this
  - Constructor enforces platform-specific delay from last test (0ms on macOS, 1000ms elsewhere)
  - Dispose() updates the last test completion time
  - Uses a static semaphore for thread safety
  - Detects platform using `OperatingSystem.IsMacOS()`

- **`IntegrationTestCollection.cs`**:
  - Defines the "Integration Tests" collection
  - Ensures tests in this collection don't run in parallel
  
- **All 17 integration test classes**:
  - Inherit from `RateLimitedIntegrationTest`
  - Marked with `[Collection("Integration Tests")]`

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
The `xunit.runner.json` file disables parallelization:
```json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false
}
```

This provides defense-in-depth, ensuring sequential execution even if collection attributes are omitted.

## Expected Behavior
With this implementation:

**On macOS (local development):**
- **No delays** between integration tests
- Tests complete quickly (~20-30 seconds for all 183 tests)
- Fast feedback loop for developers

**In CI/Linux environments:**
- **97 integration tests** × 1000ms = at least **97 seconds** of delays
- Plus actual test execution time (~30-60 seconds)
- **Total: ~2-3 minutes per target framework** for all tests
- Unit tests (86 tests) run quickly without delays