using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using Microsoft.Kiota.Serialization.Json;
using System.Text;

namespace Descope.Test.Helpers;

/// <summary>
/// Mock request adapter for testing Kiota-generated clients.
/// Based on Microsoft's Kiota testing guide: https://learn.microsoft.com/en-us/openapi/kiota/testing
/// </summary>
internal class MockRequestAdapter : IRequestAdapter
{

    private readonly Func<RequestInformation, Task<Stream>>? _mockResponseHandler;
    private readonly JsonSerializationWriterFactory _serializationWriterFactory = new();
    private readonly JsonParseNodeFactory _parseNodeFactory = new();

    public ISerializationWriterFactory SerializationWriterFactory => _serializationWriterFactory;
    public string? BaseUrl { get; set; } = "https://example.com";

    /// <summary>
    /// Creates a mock request adapter with a custom response handler.
    /// </summary>
    /// <param name="mockResponseHandler">Function that returns a stream with the mock response for a given request</param>
    private MockRequestAdapter(Func<RequestInformation, Task<Stream>>? mockResponseHandler = null)
    {
        _mockResponseHandler = mockResponseHandler;
    }

    /// <summary>
    /// Creates a mock request adapter that returns a specific object as JSON response.
    /// This is the simplest way to mock a response - just provide the response object.
    /// </summary>
    /// <typeparam name="T">The type of response object (must be IParsable)</typeparam>
    /// <param name="responseObject">The object to return as the response</param>
    /// <returns>A configured MockRequestAdapter</returns>
    internal static MockRequestAdapter CreateWithResponse<T>(T responseObject) where T : IParsable
    {
        return new MockRequestAdapter(async _ => await SerializeToStreamAsync(responseObject));
    }

    /// <summary>
    /// Creates a mock request adapter with request validation and response.
    /// Use this when you need to assert on the request before returning a response.
    /// The request body will be automatically deserialized and passed to the asserter.
    /// The asserter function returns the response object to be returned.
    /// </summary>
    /// <typeparam name="TRequest">The type of request body object (must be IParsable)</typeparam>
    /// <typeparam name="TResponse">The type of response object (must be IParsable)</typeparam>
    /// <param name="asserter">Function to validate the request and deserialized request body, and return the response</param>
    /// <returns>A configured MockRequestAdapter</returns>
    internal static MockRequestAdapter CreateWithAsserter<TRequest, TResponse>(Func<RequestInformation, TRequest?, TResponse> asserter)
        where TRequest : IParsable, new()
        where TResponse : IParsable
    {
        return new MockRequestAdapter(async requestInfo =>
        {
            ParsableFactory<TRequest> factory = (IParseNode parseNode) => new TRequest();
            var requestBody = GetRequestBody<TRequest>(requestInfo, factory);
            var responseObject = asserter(requestInfo, requestBody);
            return await SerializeToStreamAsync(responseObject);
        });
    }

    /// <summary>
    /// Creates a mock request adapter that throws a DescopeException with the specified error details.
    /// Use this to test error handling scenarios at the adapter level.
    /// Note: For HTTP-level error testing, use TestDescopeClientFactory.CreateWithError instead.
    /// </summary>
    /// <param name="errorCode">The error code to include in the exception</param>
    /// <param name="errorDescription">The error description to include in the exception</param>
    /// <param name="errorMessage">Optional error message to include in the exception</param>
    /// <returns>A configured MockRequestAdapter that throws errors</returns>
    internal static MockRequestAdapter CreateWithError(string errorCode, string errorDescription, string? errorMessage = null)
    {
        return new MockRequestAdapter(_ =>
        {
            var errorDetails = new ErrorDetails(errorCode, errorDescription, errorMessage);
            throw new DescopeException(errorDetails);
        });
    }

    /// <summary>
    /// Creates a mock request adapter that returns an empty/no-content response (200 OK with no body).
    /// Use this for endpoints that return void or no meaningful response.
    /// </summary>
    /// <returns>A configured MockRequestAdapter</returns>
    internal static MockRequestAdapter CreateWithEmptyResponse()
    {
        return new MockRequestAdapter(_ => Task.FromResult(new MemoryStream() as Stream));
    }

    /// <summary>
    /// Creates an empty mock request adapter (no responses configured).
    /// Use this for scenarios where you're testing error conditions or don't need a response.
    /// </summary>
    public MockRequestAdapter() : this(null)
    {
    }

    // Helper methods

    /// <summary>
    /// Helper method to serialize an IParsable object to a stream.
    /// This encapsulates all the low-level serialization details.
    /// </summary>
    private static async Task<Stream> SerializeToStreamAsync<T>(T obj) where T : IParsable
    {
        var writer = new JsonSerializationWriter();
        writer.WriteObjectValue(null, obj);
        var content = writer.GetSerializedContent();
        var ms = new MemoryStream();
        await content.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Helper method to extract and deserialize the request body from a RequestInformation.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the request body to</typeparam>
    /// <param name="requestInfo">The request information containing the body</param>
    /// <param name="factory">Factory method to create the object from the parse node</param>
    /// <returns>The deserialized request body, or null if there is no content</returns>
    private static T? GetRequestBody<T>(RequestInformation requestInfo, ParsableFactory<T> factory) where T : IParsable
    {
        if (requestInfo.Content == null)
        {
            return default;
        }

        var stream = new MemoryStream();
        requestInfo.Content.CopyTo(stream);
        stream.Position = 0;

        var parseNodeFactory = new JsonParseNodeFactory();
        var parseNode = parseNodeFactory.GetRootParseNodeAsync("application/json", stream).GetAwaiter().GetResult();
        return parseNode.GetObjectValue(factory);
    }

    // IRequestAdapter implementation

    public async Task<ModelType?> SendAsync<ModelType>(
        RequestInformation requestInfo,
        ParsableFactory<ModelType> factory,
        Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
        CancellationToken cancellationToken = default) where ModelType : IParsable
    {
        if (_mockResponseHandler == null)
        {
            throw new InvalidOperationException("No mock response handler configured");
        }

        var responseStream = await _mockResponseHandler(requestInfo);
        var parseNode = await _parseNodeFactory.GetRootParseNodeAsync("application/json", responseStream, cancellationToken);
        var result = parseNode.GetObjectValue(factory);
        return result;
    }

    public async Task<IEnumerable<ModelType>?> SendCollectionAsync<ModelType>(
        RequestInformation requestInfo,
        ParsableFactory<ModelType> factory,
        Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
        CancellationToken cancellationToken = default) where ModelType : IParsable
    {
        // Descope API endpoints response objects(e.g., UsersResponse with a Users property) rather than raw collections,
        // so we do not need to implement this method.
        throw new NotImplementedException();
    }

    public Task<ModelType?> SendPrimitiveAsync<ModelType>(
        RequestInformation requestInfo,
        Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
        CancellationToken cancellationToken = default)
    {
        // For endpoints that return primitives (e.g., void/empty responses),
        // we just return the default value for the type
        return Task.FromResult(default(ModelType));
    }

    public Task<IEnumerable<ModelType>?> SendPrimitiveCollectionAsync<ModelType>(
        RequestInformation requestInfo,
        Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
        CancellationToken cancellationToken = default)
    {
        // Descope API endpoints response objects(e.g., UsersResponse with a Users property) rather than raw primitive collections,
        // so we do not need to implement this method.
        throw new NotImplementedException();
    }

    public Task SendNoContentAsync(
        RequestInformation requestInfo,
        Dictionary<string, ParsableFactory<IParsable>>? errorMapping = null,
        CancellationToken cancellationToken = default)
    {
        // For endpoints that return no content, just complete successfully
        if (_mockResponseHandler != null)
        {
            // Call the handler to allow for assertions, but ignore the result
            _ = _mockResponseHandler(requestInfo);
        }
        return Task.CompletedTask;
    }

    public void EnableBackingStore(IBackingStoreFactory backingStoreFactory)
    {
        // Not needed for tests
    }

    public Task<ModelType?> ConvertToNativeRequestAsync<ModelType>(RequestInformation requestInfo, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
