using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Descope.Test.UnitTests.Internal;

public class DescopeErrorResponseHandlerTests : IDisposable
{
    private readonly TimeSpan[] _originalDelays;

    public DescopeErrorResponseHandlerTests()
    {
        // Save the original delays and replace with zero-length delays so tests don't wait
        _originalDelays = DescopeErrorResponseHandler.RetryDelays;
        DescopeErrorResponseHandler.RetryDelays = new[]
        {
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero,
        };
    }

    public void Dispose()
    {
        DescopeErrorResponseHandler.RetryDelays = _originalDelays;
    }

    [Fact]
    public void RetryDelayConfig_ShouldHaveExpectedValues()
    {
        Assert.Equal(3, _originalDelays.Length);
        Assert.Equal(TimeSpan.FromMilliseconds(100), _originalDelays[0]);
        Assert.Equal(TimeSpan.FromSeconds(5), _originalDelays[1]);
        Assert.Equal(TimeSpan.FromSeconds(5), _originalDelays[2]);
    }

    [Theory]
    [InlineData(503)]
    [InlineData(521)]
    [InlineData(522)]
    [InlineData(524)]
    [InlineData(530)]
    public async Task SendAsync_OnRetryableStatusCode_RetriesAndSucceeds(int statusCode)
    {
        // Arrange: first response retryable, second response 200 OK
        var innerHandler = new SequentialMessageHandler(
            new HttpResponseMessage((HttpStatusCode)statusCode),
            new HttpResponseMessage(HttpStatusCode.OK)
        );

        var handler = new DescopeErrorResponseHandler { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.descope.com/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, innerHandler.CallCount);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(502)]
    public async Task SendAsync_OnNonRetryableStatusCode_DoesNotRetry(int statusCode)
    {
        // Arrange: non-retryable error response with valid error body
        var errorJson = """{"errorCode":"E0","errorDescription":"error"}""";
        var innerHandler = new SequentialMessageHandler(
            new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
            }
        );

        var handler = new DescopeErrorResponseHandler { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.descope.com/test");

        // Act & Assert: should throw without retrying
        await Assert.ThrowsAsync<DescopeException>(
            () => invoker.SendAsync(request, CancellationToken.None));
        Assert.Equal(1, innerHandler.CallCount);
    }

    [Fact]
    public async Task SendAsync_OnRetryableStatusCode_RetriesUpToThreeTimes()
    {
        // Arrange: all four responses (1 original + 3 retries) return 503
        var errorJson = """{"errorCode":"E503","errorDescription":"service unavailable"}""";
        var innerHandler = new SequentialMessageHandler(
            new HttpResponseMessage((HttpStatusCode)503)
            {
                Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage((HttpStatusCode)503)
            {
                Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage((HttpStatusCode)503)
            {
                Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage((HttpStatusCode)503)
            {
                Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
            }
        );

        var handler = new DescopeErrorResponseHandler { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.descope.com/test");

        // Act & Assert: exhausted retries should throw DescopeException
        await Assert.ThrowsAsync<DescopeException>(
            () => invoker.SendAsync(request, CancellationToken.None));

        // 1 original + 3 retries = 4 total calls
        Assert.Equal(4, innerHandler.CallCount);
    }

    [Fact]
    public async Task SendAsync_SucceedsOnThirdRetry()
    {
        // Arrange: three retryable responses then success
        var innerHandler = new SequentialMessageHandler(
            new HttpResponseMessage((HttpStatusCode)503),
            new HttpResponseMessage((HttpStatusCode)522),
            new HttpResponseMessage((HttpStatusCode)530),
            new HttpResponseMessage(HttpStatusCode.OK)
        );

        var handler = new DescopeErrorResponseHandler { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.descope.com/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, innerHandler.CallCount);
    }

    [Fact]
    public async Task SendAsync_OnSuccess_DoesNotRetry()
    {
        // Arrange
        var innerHandler = new SequentialMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
        );

        var handler = new DescopeErrorResponseHandler { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.descope.com/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, innerHandler.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenCancelledDuringRetryDelay_ThrowsOperationCancelled()
    {
        // Use real delays so the cancellation fires during the wait
        DescopeErrorResponseHandler.RetryDelays = new[]
        {
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10),
        };

        var firstCallDone = new TaskCompletionSource<bool>();
        var innerHandler = new CallbackMessageHandler(request =>
        {
            firstCallDone.TrySetResult(true);
            return new HttpResponseMessage((HttpStatusCode)503);
        });

        var handler = new DescopeErrorResponseHandler { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.descope.com/test");

        using var cts = new CancellationTokenSource();

        var sendTask = invoker.SendAsync(request, cts.Token);

        // Cancel once the first response has been received, before the retry delay fires
        await firstCallDone.Task;
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask);
        Assert.Equal(1, innerHandler.CallCount);
    }

    // --- Helpers ---

    /// <summary>
    /// Returns a predefined sequence of responses, one per call.
    /// </summary>
    private sealed class SequentialMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage[] _responses;
        private int _callIndex;

        public int CallCount => _callIndex;

        public SequentialMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_callIndex >= _responses.Length)
                throw new InvalidOperationException("No more responses configured.");
            return Task.FromResult(_responses[_callIndex++]);
        }
    }

    /// <summary>
    /// Calls a callback on each request to produce the response.
    /// </summary>
    private sealed class CallbackMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _callback;

        public int CallCount { get; private set; }

        public CallbackMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> callback)
        {
            _callback = callback;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_callback(request));
        }
    }
}
