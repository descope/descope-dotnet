using Descope.Internal.Management;
using Descope.Internal.Auth;
using System.Diagnostics;
using System.Reflection;

namespace Descope
{
    public class DescopeClient
    {
        public IAuthentication Auth { get; }
        public IManagement Management { get; }

        public DescopeClient(DescopeConfig descopeConfig)
        {
            var httpClient = new Internal.HttpClient(descopeConfig);
            var managementKey = descopeConfig.ManagementKey ?? "";

            Auth = new Authentication(httpClient);
            Management = new Management(httpClient, managementKey);
        }
    }

    public static class SdkInfo
    {
        public static string Name { get; } = "dotnet";
        public static string Version { get; } = Assembly.GetAssembly(typeof(DescopeClient))?.GetName()?.Version?.ToString() ?? "Unknown";
        public static string DotNetVersion { get; } = Environment.Version.ToString();
    }
}
