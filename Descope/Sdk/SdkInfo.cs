using System;
using System.Reflection;

namespace Descope;

/// <summary>
/// Provides information about the Descope SDK.
/// </summary>
public static class SdkInfo
{
    /// <summary>
    /// Gets the SDK name.
    /// </summary>
    public static string Name { get; } = "dotnet";

    /// <summary>
    /// Gets the SDK version.
    /// </summary>
    public static string Version { get; } = Assembly.GetAssembly(typeof(IDescopeClient))?.GetName()?.Version?.ToString() ?? "Unknown";

    /// <summary>
    /// Gets the .NET runtime version.
    /// </summary>
    public static string DotNetVersion { get; } = Environment.Version.ToString();
}
