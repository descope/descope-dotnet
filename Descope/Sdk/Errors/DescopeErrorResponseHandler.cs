using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;

namespace Descope;

/// <summary>
/// HTTP middleware that intercepts error responses and converts them to DescopeException.
/// This handler parses the error response body for Descope-specific error details.
/// </summary>
internal class DescopeErrorResponseHandler : DelegatingHandler
{
    /// <summary>
    /// Sends an HTTP request and handles error responses by converting them to DescopeException.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // If the response is not successful, try to parse it as a Descope error
        if (!response.IsSuccessStatusCode)
        {
            await ThrowDescopeExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    /// <summary>
    /// Parses the error response and throws a DescopeException with populated error details.
    /// </summary>
    private static async Task ThrowDescopeExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        ErrorDetails? errorDetails = null;

        try
        {
            // Try to read and parse the response body as JSON
            if (response.Content != null)
            {
                var responseBody = await response.Content.ReadAsStringAsync(
#if NET5_0_OR_GREATER
                    cancellationToken
#endif
                ).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    // Parse the error response using JsonSerializer
                    errorDetails = JsonSerializer.Deserialize<ErrorDetails>(
                        responseBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                }
            }
        }
        catch
        {
            // If we can't parse the error response, fall through to throw a generic error
        }

        // If we successfully parsed error details, throw a DescopeException with them
        if (errorDetails != null && !string.IsNullOrEmpty(errorDetails.ErrorCode))
        {
            throw new DescopeException(errorDetails);
        }

        // Otherwise, throw a generic DescopeException with the HTTP status information
        var genericError = new ErrorDetails(
            errorCode: $"HTTP{(int)response.StatusCode}",
            errorDescription: $"The server returned an error: {response.ReasonPhrase ?? response.StatusCode.ToString()}",
            errorMessage: $"HTTP {(int)response.StatusCode}"
        );

        throw new DescopeException(genericError);
    }
}
