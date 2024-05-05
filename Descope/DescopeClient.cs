using Descope.Internal.Management;
using Descope.Internal.Auth;

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
        public static string Version { get; } = "0.2.0";
    }
}
