using Microsoft.Extensions.Configuration;

namespace Descope.Test.Integration
{
    internal class IntegrationTestSetup
    {
        internal static DescopeClient InitDescopeClient()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettingsTest.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var projectId = configuration["AppSettings:ProjectId"] ?? throw new ApplicationException("Can't run tests without a project ID");
            var managementKey = configuration["AppSettings:ManagementKey"] ?? throw new ApplicationException("Can't run tests without a management key");
            var baseUrl = configuration["AppSettings:BaseURL"];
            var isUnsafe = bool.Parse(configuration["AppSettings:Unsafe"] ?? "false");

            var config = new DescopeConfig(projectId: projectId)
            {
                ManagementKey = managementKey,
                BaseURL = baseUrl,
                Unsafe = isUnsafe,
            };

            return new DescopeClient(config);
        }

        internal static async Task<SignedInTestUser> InitTestUser(DescopeClient descopeClient)
        {
            var loginId = Guid.NewGuid().ToString();
            var user = await descopeClient.Management.User.Create(loginId: loginId, new UserRequest()
            {
                phone = "+972555555555",
                verifiedPhone = true,
            }, testUser: true);

            var generatedOtp = await descopeClient.Management.User.GenerateOtpForTestUser(DeliveryMethod.sms, loginId);
            var authInfo = await descopeClient.Auth.Otp.Verify(DeliveryMethod.sms, loginId, generatedOtp.Code);
            return new SignedInTestUser(user, authInfo);
        }

    }

    internal class SignedInTestUser
    {
        internal DescopeUser User { get; }
        internal AuthenticationResponse AuthInfo { get; }
        internal SignedInTestUser(DescopeUser user, AuthenticationResponse authInfo)
        {
            User = user;
            AuthInfo = authInfo;
        }
    }
}
