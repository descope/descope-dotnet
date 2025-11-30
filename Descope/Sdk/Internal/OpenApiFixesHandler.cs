using System.Text;
using System.Text.Json;

namespace Descope.Internal;

/// <summary>
/// Fixes OpenAPI inconsistency for endpoints which use the protobuf "response_body" option, which causes the response to return "flat" fields in the root object instead of nested under a specific field.
/// Affected endpoints:
/// - GET /v1/mgmt/sso/idp/app/load
/// </summary>
internal class OpenApiFixesHandler : DelegatingHandler
{
    private static readonly string LoadSsoAppEndpoint = "/v1/mgmt/sso/idp/app/load";
    private static readonly string LoadTenantEndpoint = "/v1/mgmt/tenant";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        var requestPath = request.RequestUri?.AbsolutePath ?? "";

        // Fix SSO app load endpoint
        if (requestPath.EndsWith(LoadSsoAppEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "app", "id", cancellationToken);
        }
        // Fix tenant load endpoint
        else if (requestPath.EndsWith(LoadTenantEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            response = await WrapResponseInField(response, "tenant", "id", cancellationToken);
        }

        return response;
    }

    private async Task<HttpResponseMessage> WrapResponseInField(
        HttpResponseMessage response,
        string wrapperFieldName,
        string detectionFieldName,
        CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        var content = await response.Content.ReadAsStringAsync();
#else
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
#endif

        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                // Parse the response to check if it needs fixing
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // If response has the detection field at root level (not wrapped), fix it
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(detectionFieldName, out _))
                {
                    // Check if wrapper field already exists
                    if (!root.TryGetProperty(wrapperFieldName, out _))
                    {
                        // Wrap the entire response in the wrapper field
                        var wrappedResponse = JsonSerializer.Serialize(new Dictionary<string, JsonElement>
                        {
                            [wrapperFieldName] = root
                        });

                        response.Content = new StringContent(wrappedResponse, Encoding.UTF8, "application/json");
                    }
                }
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return original response
                // Don't throw - let the normal error handling take over
            }
        }

        return response;
    }
}
