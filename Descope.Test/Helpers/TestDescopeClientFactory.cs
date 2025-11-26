using Descope.Sdk;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Descope.Test.Helpers;

/// <summary>
/// Helper class for creating DescopeClient instances for testing, allow to mock responses and assert requests.
/// Simplifies test setup by providing high-level factory methods.
/// </summary>
public static class TestDescopeClientFactory
{
    private const string DefaultTestProjectId = "test_project_id";

    /// <summary>
    /// Creates a mock DescopeClient that returns a specific object as JSON response.
    /// This is the simplest way to mock a response - just provide the response object.
    /// </summary>
    /// <typeparam name="T">The type of response object (must be IParsable)</typeparam>
    /// <param name="responseObject">The object to return as the response</param>
    /// <param name="projectId">Optional project ID (defaults to "test_project_id")</param>
    /// <returns>A configured IDescopeClient for testing</returns>
    public static IDescopeClient CreateWithResponse<T>(T responseObject, string? projectId = null) where T : IParsable
    {
        var mockAdapter = MockRequestAdapter.CreateWithResponse(responseObject);
        var options = new DescopeClientOptions { ProjectId = projectId ?? DefaultTestProjectId };
        return DescopeManagementClientFactory.CreateForTest(mockAdapter, mockAdapter, options);
    }

    /// <summary>
    /// Creates a mock DescopeClient with request validation and response.
    /// Use this when you need to assert on the request before returning a response.
    /// The request body will be automatically deserialized and passed to the asserter.
    /// The asserter function returns the response object to be returned.
    /// </summary>
    /// <typeparam name="TRequest">The type of request body object (must be IParsable)</typeparam>
    /// <typeparam name="TResponse">The type of response object (must be IParsable)</typeparam>
    /// <param name="asserter">Function to validate the request and deserialized request body, and return the response</param>
    /// <param name="projectId">Optional project ID (defaults to "test_project_id")</param>
    /// <returns>A configured IDescopeClient for testing</returns>
    public static IDescopeClient CreateWithAsserter<TRequest, TResponse>(
        Func<RequestInformation, TRequest?, TResponse> asserter,
        string? projectId = null)
        where TRequest : IParsable, new()
        where TResponse : IParsable
    {
        var mockAdapter = MockRequestAdapter.CreateWithAsserter<TRequest, TResponse>(asserter);
        var options = new DescopeClientOptions { ProjectId = projectId ?? DefaultTestProjectId };
        return DescopeManagementClientFactory.CreateForTest(mockAdapter, mockAdapter, options);
    }
}
