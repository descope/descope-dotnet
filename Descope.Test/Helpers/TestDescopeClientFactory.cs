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
    /// Creates a mock DescopeClient that returns an empty/no-content response (200 OK with no body).
    /// Use this for testing endpoints that return void or no meaningful response, such as delete operations.
    /// </summary>
    /// <param name="projectId">Optional project ID (defaults to "test_project_id")</param>
    /// <returns>A configured IDescopeClient for testing</returns>
    public static IDescopeClient CreateWithEmptyResponse(string? projectId = null)
    {
        var mockAdapter = MockRequestAdapter.CreateWithEmptyResponse();
        var options = new DescopeClientOptions { ProjectId = projectId ?? DefaultTestProjectId };
        return DescopeManagementClientFactory.CreateForTest(mockAdapter, mockAdapter, options, new HttpClient());
    }

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
        return DescopeManagementClientFactory.CreateForTest(mockAdapter, mockAdapter, options, new HttpClient());
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
        var mockAdapter = MockRequestAdapter.CreateWithAsserter(asserter);
        var options = new DescopeClientOptions { ProjectId = projectId ?? DefaultTestProjectId };
        return DescopeManagementClientFactory.CreateForTest(mockAdapter, mockAdapter, options, new HttpClient());
    }


    /// <summary>
    /// Creates a mock DescopeClient that throws a DescopeException with the specified error details at the HTTP level.
    /// This simulates an actual HTTP error response from the server, including proper error parsing.
    /// Use this to test error handling scenarios and ensure your code properly catches and handles Descope errors.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to return (e.g., HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest)</param>
    /// <param name="errorCode">The error code to include in the exception (e.g., "E062504")</param>
    /// <param name="errorDescription">The error description to include in the exception (e.g., "Token expired")</param>
    /// <param name="errorMessage">Optional error message to include in the exception (e.g., "Failed to load magic link token")</param>
    /// <param name="projectId">Optional project ID (defaults to "test_project_id")</param>
    /// <returns>A configured IDescopeClient for testing that throws HTTP-level errors</returns>
    public static IDescopeClient CreateWithError(
        System.Net.HttpStatusCode statusCode,
        string errorCode,
        string errorDescription,
        string? errorMessage = null,
        string? projectId = null)
    {
        var options = new DescopeClientOptions { ProjectId = projectId ?? DefaultTestProjectId, BaseUrl = "https://test.example.com" };

        // Create a mock HTTP handler that returns an error response
        var mockHttpHandler = new MockHttpErrorMessageHandler(statusCode, errorCode, errorDescription, errorMessage);

        // Wrap it with the error handler to get proper DescopeException parsing
        var errorHandler = new DescopeErrorResponseHandler
        {
            InnerHandler = mockHttpHandler
        };

        var httpClient = new HttpClient(errorHandler);

        // Create authentication providers
        var mgmtAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, null);
        var authAuthProvider = new DescopeAuthenticationProvider(options.ProjectId, null);

        // Create real request adapters that use the mock HttpClient
        var mgmtAdapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(mgmtAuthProvider, httpClient: httpClient)
        {
            BaseUrl = options.BaseUrl
        };

        var authAdapter = new Microsoft.Kiota.Http.HttpClientLibrary.HttpClientRequestAdapter(authAuthProvider, httpClient: httpClient)
        {
            BaseUrl = options.BaseUrl
        };

        return DescopeManagementClientFactory.CreateForTest(authAdapter, mgmtAdapter, options, httpClient);
    }

    /// <summary>
    /// Mock HTTP message handler that returns error responses for testing.
    /// </summary>
    private class MockHttpErrorMessageHandler : HttpMessageHandler
    {
        private readonly System.Net.HttpStatusCode _statusCode;
        private readonly string _errorCode;
        private readonly string _errorDescription;
        private readonly string? _errorMessage;

        public MockHttpErrorMessageHandler(System.Net.HttpStatusCode statusCode, string errorCode, string errorDescription, string? errorMessage)
        {
            _statusCode = statusCode;
            _errorCode = errorCode;
            _errorDescription = errorDescription;
            _errorMessage = errorMessage;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var errorResponse = new ErrorDetails(_errorCode, _errorDescription, _errorMessage);
            var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
