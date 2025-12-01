using Descope.Mgmt.Models;
using Descope.Mgmt.Models.Managementv1;
using Microsoft.Kiota.Abstractions;

namespace Descope;

/// <summary>
/// Extension methods for management operations to simplify common patterns.
/// </summary>
public static class MgmtExtensions
{
    /// <summary>
    /// Updates password settings by accepting a GetPasswordSettingsResponse and converting it to a ConfigurePasswordSettingsRequest.
    /// This allows for easier modification of existing settings by retrieving current settings and updating specific fields.
    /// </summary>
    /// <param name="requestBuilder">The password settings request builder</param>
    /// <param name="settings">The password settings response to use as the basis for the update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the response</returns>
    public static async Task<Stream?> PostWithSettingsResponseAsync(
        this Descope.Mgmt.V1.Mgmt.Password.Settings.SettingsRequestBuilder requestBuilder,
        GetPasswordSettingsResponse settings,
        CancellationToken cancellationToken = default)
    {
        if (settings == null)
        {
            throw new DescopeException("Settings are required for updating password settings");
        }

        var request = new ConfigurePasswordSettingsRequest
        {
            Enabled = settings.Enabled,
            MinLength = settings.MinLength,
            Lowercase = settings.Lowercase,
            Uppercase = settings.Uppercase,
            Number = settings.Number,
            NonAlphanumeric = settings.NonAlphanumeric,
            EnablePasswordStrength = settings.EnablePasswordStrength,
            PasswordStrengthScore = settings.PasswordStrengthScore,
            Expiration = settings.Expiration,
            ExpirationWeeks = settings.ExpirationWeeks,
            Reuse = settings.Reuse,
            ReuseAmount = settings.ReuseAmount,
            Lock = settings.Lock,
            LockAttempts = settings.LockAttempts,
            TempLock = settings.TempLock,
            TempLockAttempts = settings.TempLockAttempts,
            TempLockDuration = settings.TempLockDuration,
            TenantId = settings.TenantId
        };

        return await requestBuilder.PostAsync(request, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads password settings for a specific tenant by tenant ID.
    /// This is a convenience method that simplifies the common pattern of loading password settings for a tenant.
    /// </summary>
    /// <param name="requestBuilder">The password settings request builder</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>GetPasswordSettingsResponse containing the password settings information</returns>
    public static async Task<GetPasswordSettingsResponse?> LoadWithTenantIdAsync(
        this Descope.Mgmt.V1.Mgmt.Password.Settings.SettingsRequestBuilder requestBuilder,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new DescopeException("Tenant ID is required for loading password settings");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.TenantId = tenantId;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Imports a project by accepting an ExportSnapshotResponse and converting it to an ImportSnapshotRequest.
    /// This allows for directly importing an exported project without manual conversion.
    /// </summary>
    /// <param name="requestBuilder">The project import request builder</param>
    /// <param name="exportedProject">The exported project response to import</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the response</returns>
    public static async Task<Stream?> PostWithExportedProjectAsync(
        this Descope.Mgmt.V1.Mgmt.Project.Import.ImportRequestBuilder requestBuilder,
        ExportSnapshotResponse exportedProject,
        CancellationToken cancellationToken = default)
    {
        if (exportedProject == null)
        {
            throw new DescopeException("Exported project is required for importing");
        }

        // Create ImportSnapshotRequest_files and copy AdditionalData from exported files
        var importFiles = new ImportSnapshotRequest_files();
        if (exportedProject.Files?.AdditionalData != null)
        {
            foreach (var kvp in exportedProject.Files.AdditionalData)
            {
                importFiles.AdditionalData[kvp.Key] = kvp.Value;
            }
        }

        var request = new ImportSnapshotRequest
        {
            Files = importFiles
        };

        return await requestBuilder.PostAsync(request, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads a user by identifier (userId or loginId)
    /// This is a convenience method that simplifies the common pattern of loading a user by their identifier.
    /// </summary>
    /// <param name="requestBuilder">The user request builder</param>
    /// <param name="identifier">The user identifier (login ID, email, phone, or user ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>UserResponse containing the user information</returns>
    public static async Task<UserResponse?> LoadAsync(
        this Descope.Mgmt.V1.Mgmt.User.UserRequestBuilder requestBuilder,
        string identifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new DescopeException("Identifier is required for loading a user");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Identifier = identifier;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads an access key by ID.
    /// This is a convenience method that simplifies the common pattern of loading an access key by its ID.
    /// </summary>
    /// <param name="requestBuilder">The access key request builder</param>
    /// <param name="id">The access key ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AccessKeyResponse containing the access key information</returns>
    public static async Task<AccessKeyResponse?> LoadAsync(
        this Descope.Mgmt.V1.Mgmt.Accesskey.AccesskeyRequestBuilder requestBuilder,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for loading an access key");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Id = id;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads an SSO application by ID.
    /// This is a convenience method that simplifies the common pattern of loading an SSO application by its ID.
    /// </summary>
    /// <param name="requestBuilder">The SSO app load request builder</param>
    /// <param name="id">The SSO application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadSSOApplicationResponse containing the SSO application information</returns>
    public static async Task<LoadSSOApplicationResponse?> LoadAsync(
        this Descope.Mgmt.V1.Mgmt.Sso.Idp.App.Load.LoadRequestBuilder requestBuilder,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for loading an SSO application");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Id = id;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads SSO settings by tenant ID.
    /// This is a convenience method that simplifies the common pattern of loading SSO settings for a tenant.
    /// </summary>
    /// <param name="requestBuilder">The SSO settings request builder</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadSSOSettingsResponse containing the SSO settings information</returns>
    public static async Task<LoadSSOSettingsResponse?> LoadAsync(
        this Descope.Mgmt.V2.Mgmt.Sso.Settings.SettingsRequestBuilder requestBuilder,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new DescopeException("Tenant ID is required for loading SSO settings");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.TenantId = tenantId;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads a third-party application by ID.
    /// This is a convenience method that simplifies the common pattern of loading a third-party application by its ID.
    /// </summary>
    /// <param name="requestBuilder">The third-party app load request builder</param>
    /// <param name="id">The third-party application ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadThirdPartyApplicationResponse containing the third-party application information</returns>
    public static async Task<LoadThirdPartyApplicationResponse?> LoadWithAppIdAsync(
        this Descope.Mgmt.V1.Mgmt.Thirdparty.App.Load.LoadRequestBuilder requestBuilder,
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new DescopeException("ID is required for loading a third-party application");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.Id = id;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Loads a third-party application by client ID.
    /// This is a convenience method that simplifies the common pattern of loading a third-party application by its client ID.
    /// </summary>
    /// <param name="requestBuilder">The third-party app load request builder</param>
    /// <param name="clientId">The third-party application client ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LoadThirdPartyApplicationResponse containing the third-party application information</returns>
    public static async Task<LoadThirdPartyApplicationResponse?> LoadWithClientIdAsync(
        this Descope.Mgmt.V1.Mgmt.Thirdparty.App.Load.LoadRequestBuilder requestBuilder,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            throw new DescopeException("Client ID is required for loading a third-party application");
        }

        return await requestBuilder.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.ClientId = clientId;
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes SSO settings for a tenant by tenant ID.
    /// This is a convenience method that simplifies the common pattern of deleting SSO settings for a tenant.
    /// </summary>
    /// <param name="requestBuilder">The SSO settings request builder</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the response</returns>
    public static async Task<Stream?> DeleteWithTenantIdAsync(
        this Descope.Mgmt.V1.Mgmt.Sso.Settings.SettingsRequestBuilder requestBuilder,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new DescopeException("Tenant ID is required for deleting SSO settings");
        }

        return await requestBuilder.DeleteAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.TenantId = tenantId;
        }, cancellationToken: cancellationToken);
    }
}
