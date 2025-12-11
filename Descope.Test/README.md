# Descope .NET SDK Unit Tests

This directory contains unit tests for the Descope .NET SDK using xUnit and Kiota-based mocking.

## Overview

The tests use a mock request adapter approach following Microsoft's [Kiota testing documentation](https://learn.microsoft.com/en-us/openapi/kiota/testing). This allows us to test the SDK behavior without making actual HTTP calls.

## Project Structure

```
Descope.Test/
├── Descope.Test.csproj             # Test project configuration
├── Helpers/
│   ├── MockRequestAdapter.cs       # Internal mock implementation of IRequestAdapter, handling kiota response mocking
│   └── TestDescopeClientFactory.cs # High-level helper for creating mock DescopeClient instances
└── UnitTests/
    ├── Authentication/             # Unit tests for Authentication API
    └── Management/                 # Unit tests for Management API
```

## Testing Approach

### TestDescopeClientFactory Helper

The `TestDescopeClientFactory` class provides a high-level API for creating mock `DescopeClient` instances for testing.
A new mock client should be created before each request.
There are two main methods to create a mock client:

**1. Simple Response Mocking** - Just provide the response object:
```csharp
var mockResponse = new JWTResponse { SessionJwt = "token", ... };
var descopeClient = TestDescopeClientFactory.CreateWithResponse(mockResponse);
```

**2. Request and Body Validation** - Validate the request and body, then return a response:
```csharp
var descopeClient = TestDescopeClientFactory.CreateWithAsserter<MyRequestType, JWTResponse>((requestInfo, requestBody) =>
{
    // Validate request metadata
    requestInfo.HttpMethod.Should().Be(Method.POST);
    requestInfo.URI.AbsolutePath.Should().EndWith("/endpoint");
    
    // Validate request body - automatically deserialized!
    requestBody.Should().NotBeNull();
    requestBody!.Field.Should().Be("expected_value");
    
    // Return the response
    return new JWTResponse { SessionJwt = "token", ... };
});
```

Both methods accept an optional `projectId` parameter (defaults to `"test_project_id"`).

All serialization, streaming, and other low-level details are handled internally.

## Running Tests

### Run all tests

```bash
dotnet test Descope.Test/Descope.Test.csproj
```

### Run specific test class

```bash
dotnet test Descope.Test/Descope.Test.csproj --filter "FullyQualifiedName~MagicLinkTests"
```

### Run specific test

```bash
dotnet test Descope.Test/Descope.Test.csproj --filter "FullyQualifiedName~MagicLink_Verify_Success"
```

### Run with detailed output

```bash
dotnet test Descope.Test/Descope.Test.csproj --logger "console;verbosity=detailed"
```

## Dependencies

- **xUnit** - Testing framework
- **FluentAssertions** - Fluent assertion library for more readable tests
- **Microsoft.Kiota.Abstractions** - Kiota abstractions for request handling
- **Microsoft.Kiota.Serialization.Json** - JSON serialization for Kiota

## Adding New Tests

To add tests for a new endpoint:

1. Create a new test class in the appropriate namespace (`Authentication/` or `Management/`)
2. Use `TestDescopeClientFactory.CreateWithResponse()` or `TestDescopeClientFactory.CreateWithAsserter()` to create a mock client before each request sent in the test
3. Write tests following the Arrange-Act-Assert pattern
4. Use FluentAssertions for readable assertions

## Best Practices

1. **Isolate tests** - Each test should be independent and not rely on others
2. **Use descriptive names** - Test names should clearly indicate what is being tested
3. **Test edge cases** - Include tests for error conditions, null values, and boundary cases
4. **Mock minimally** - Only mock what's necessary for the test
5. **Single mock per request** - Create a new mock client for each request if needed
6. **Verify behavior** - Test both the response and that requests are formed correctly

## C#/.NET Idiomatic Patterns

These tests follow C#/.NET best practices:

- **Async/await** - Proper async testing patterns
- **Nullable reference types** - Enabled for better null safety
- **XML documentation** - All public members documented
- **FluentAssertions** - Idiomatic .NET assertion style
- **xUnit** - Industry-standard .NET testing framework
- **Reflection for internals** - Allowed for testing internal constructors, but should be used judiciously and avoided if possible
