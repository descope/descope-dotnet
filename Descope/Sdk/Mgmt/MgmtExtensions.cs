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
            throw new ArgumentNullException(nameof(settings), "Settings are required for updating password settings");
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
            throw new ArgumentNullException(nameof(exportedProject), "Exported project is required for importing");
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
}
