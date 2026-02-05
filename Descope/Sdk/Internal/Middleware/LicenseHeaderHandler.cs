using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Descope.Internal;

/// <summary>
/// Fetches license type on first mgmt request and adds x-descope-license header.
/// Fails gracefully - SDK continues without header if fetch fails.
/// </summary>
internal class LicenseHeaderHandler : DelegatingHandler
{
    private readonly string _baseUrl;
    private readonly string _projectId;
    private readonly string? _managementKey;
    private readonly ILogger? _logger;
    
    private string? _licenseType;
    private bool _licenseFetched;
    private readonly object _lock = new();
    private Task? _fetchTask;

    private const string LicenseEndpoint = "/v1/mgmt/license";
    private const string LicenseHeaderName = "x-descope-license";
    private const string ManagementPathPrefix = "/v1/mgmt";

    public LicenseHeaderHandler(string baseUrl, string projectId, string? managementKey, ILogger? logger = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _projectId = projectId;
        _managementKey = managementKey;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Only process management API requests, but skip the license endpoint itself to avoid recursion
        if (request.RequestUri != null && IsManagementRequest(request.RequestUri) && !IsLicenseRequest(request.RequestUri))
        {
            await EnsureLicenseFetchedAsync(cancellationToken);

            if (!string.IsNullOrEmpty(_licenseType))
            {
                request.Headers.Remove(LicenseHeaderName);
                request.Headers.Add(LicenseHeaderName, _licenseType);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private bool IsManagementRequest(Uri uri)
    {
        return uri.AbsolutePath.StartsWith(ManagementPathPrefix);
    }

    private bool IsLicenseRequest(Uri uri)
    {
        return uri.AbsolutePath == LicenseEndpoint || uri.AbsolutePath == LicenseEndpoint + "/";
    }

    private async Task EnsureLicenseFetchedAsync(CancellationToken cancellationToken)
    {
        if (_licenseFetched) return;

        Task? taskToAwait;
        lock (_lock)
        {
            if (_licenseFetched) return;

            if (_fetchTask == null)
            {
                _fetchTask = FetchLicenseInternalAsync(cancellationToken);
            }
            taskToAwait = _fetchTask;
        }

        await taskToAwait;
    }

    private async Task FetchLicenseInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var licenseUrl = $"{_baseUrl}{LicenseEndpoint}";
            using var request = new HttpRequestMessage(HttpMethod.Get, licenseUrl);
            
            var bearer = string.IsNullOrEmpty(_managementKey) 
                ? _projectId 
                : $"{_projectId}:{_managementKey}";
            request.Headers.Add("Authorization", $"Bearer {bearer}");

            using var response = await base.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var licenseResponse = JsonSerializer.Deserialize<LicenseResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (!string.IsNullOrEmpty(licenseResponse?.LicenseType))
                {
                    _licenseType = licenseResponse.LicenseType;
                    _logger?.LogDebug("License handshake successful. License type: {LicenseType}", _licenseType);
                }
            }
            else
            {
                _logger?.LogWarning("License handshake failed with status {StatusCode}. SDK will continue without license header.", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "License handshake failed. SDK will continue without license header.");
        }
        finally
        {
            _licenseFetched = true;
        }
    }

    private class LicenseResponse
    {
        public string? LicenseType { get; set; }
    }
}
