using System;
using System.Net.Http;

namespace Descope;

/// <summary>
/// Provides HTTP client configuration for Descope SDK.
/// Configures standard headers required by Descope API.
/// </summary>
internal static class DescopeHttpHeaders
{
    /// <summary>
    /// Configures an HttpClient with required Descope headers.
    /// This method is idempotent - calling it multiple times on the same HttpClient
    /// will not duplicate headers.
    /// </summary>
    /// <param name="httpClient">The HttpClient to configure.</param>
    /// <param name="projectId">The Descope Project ID.</param>
    public static void ConfigureHeaders(HttpClient httpClient, string projectId)
    {
        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }

        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("Project ID is required", nameof(projectId));
        }

        // Add Descope SDK headers only if they don't already exist (idempotent)
        TryAddHeader(httpClient, "x-descope-sdk-name", SdkInfo.Name);
        TryAddHeader(httpClient, "x-descope-sdk-version", SdkInfo.Version);
        TryAddHeader(httpClient, "x-descope-sdk-dotnet-version", SdkInfo.DotNetVersion);
        TryAddHeader(httpClient, "x-descope-project-id", projectId);
    }

    /// <summary>
    /// Adds a header to the HttpClient only if it doesn't already exist.
    /// </summary>
    private static void TryAddHeader(HttpClient httpClient, string name, string value)
    {
        if (!httpClient.DefaultRequestHeaders.Contains(name))
        {
            httpClient.DefaultRequestHeaders.Add(name, value);
        }
    }
}
