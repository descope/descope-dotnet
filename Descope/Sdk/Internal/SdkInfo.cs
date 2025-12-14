using System;
using System.Reflection;

namespace Descope;

/// <summary>
/// Provides information about the Descope SDK.
/// </summary>
internal static class SdkInfo
{
    /// <summary>
    /// Gets the SDK name.
    /// </summary>
    public static string Name { get; } = "dotnet";

    /// <summary>
    /// Gets the SDK version.
    /// </summary>
    public static string Version { get; } = Assembly.GetAssembly(typeof(IDescopeClient))?.GetName()?.Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// Gets the .NET runtime version.
    /// </summary>
    public static string DotNetVersion { get; } = Environment.Version.ToString();
}
