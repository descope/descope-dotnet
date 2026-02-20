using System.Text;
using System.Text.Json;

namespace Descope.Internal;

/// <summary>
/// Extracts JWTs from Set-Cookie headers (DS for session, DSR for refresh) and patches
/// them into the response body when the body fields are missing or empty.
/// This handles the "Manage in cookies" mode where the server returns JWTs only in
/// Set-Cookie headers instead of the response body.
/// </summary>
internal class CookieToBodyHandler : DelegatingHandler
{
    public const string SessionCookieName = "DS";
    public const string RefreshCookieName = "DSR";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        // Check for Set-Cookie headers with DS or DSR cookies
        string? sessionJwt = null;
        string? refreshJwt = null;

        if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            foreach (var cookieHeader in cookieHeaders)
            {
                var (name, value) = ParseCookieNameValue(cookieHeader);
                if (name == SessionCookieName && !string.IsNullOrEmpty(value))
                {
                    sessionJwt = value;
                }
                else if (name == RefreshCookieName && !string.IsNullOrEmpty(value))
                {
                    refreshJwt = value;
                }
            }
        }

        // If no relevant cookies found, skip body patching
        if (sessionJwt == null && refreshJwt == null)
        {
            return response;
        }

        // Read and patch the response body if JWT fields are missing
        if (response.Content != null)
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
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        bool needsSessionJwt = sessionJwt != null && IsNullOrEmpty(root, "sessionJwt");
                        bool needsRefreshJwt = refreshJwt != null && IsNullOrEmpty(root, "refreshJwt");

                        if (needsSessionJwt || needsRefreshJwt)
                        {
                            using var ms = new System.IO.MemoryStream();
                            using (var writer = new Utf8JsonWriter(ms))
                            {
                                writer.WriteStartObject();
                                foreach (var property in root.EnumerateObject())
                                {
                                    // Skip existing empty fields that we'll replace with cookie values
                                    if ((needsSessionJwt && property.NameEquals("sessionJwt")) ||
                                        (needsRefreshJwt && property.NameEquals("refreshJwt")))
                                    {
                                        continue;
                                    }
                                    property.WriteTo(writer);
                                }
                                if (needsSessionJwt)
                                {
                                    writer.WriteString("sessionJwt", sessionJwt);
                                }
                                if (needsRefreshJwt)
                                {
                                    writer.WriteString("refreshJwt", refreshJwt);
                                }
                                writer.WriteEndObject();
                            }

                            var patchedContent = Encoding.UTF8.GetString(ms.ToArray());
                            response.Content = new StringContent(patchedContent, Encoding.UTF8, "application/json");
                        }
                    }
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, return original response
                }
            }
        }

        return response;
    }

    private static bool IsNullOrEmpty(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return true;
        }
        return value.ValueKind == JsonValueKind.Null ||
               (value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(value.GetString()));
    }

    private static (string name, string value) ParseCookieNameValue(string cookieHeader)
    {
        // Set-Cookie format: "NAME=VALUE; Path=/; ..."
        var semicolonIndex = cookieHeader.IndexOf(';');
        var nameValuePart = semicolonIndex >= 0 ? cookieHeader.Substring(0, semicolonIndex) : cookieHeader;

        var equalsIndex = nameValuePart.IndexOf('=');
        if (equalsIndex <= 0)
        {
            return (string.Empty, string.Empty);
        }

        var name = nameValuePart.Substring(0, equalsIndex).Trim();
        var value = nameValuePart.Substring(equalsIndex + 1).Trim();
        return (name, value);
    }
}
