using System;
using System.Net.Http;

namespace Descope;

/// <summary>
/// Provides HTTP client configuration for Descope SDK.
/// Configures standard headers required by Descope API.
/// </summary>
public static class DescopeHttpClientHandler
{
    /// <summary>
    /// Configures an HttpClient with required Descope headers.
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

        // Add Descope SDK headers
        httpClient.DefaultRequestHeaders.Add("x-descope-sdk-name", SdkInfo.Name);
        httpClient.DefaultRequestHeaders.Add("x-descope-sdk-version", SdkInfo.Version);
        httpClient.DefaultRequestHeaders.Add("x-descope-sdk-dotnet-version", SdkInfo.DotNetVersion);
        httpClient.DefaultRequestHeaders.Add("x-descope-project-id", projectId);
    }
}
