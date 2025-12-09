using Microsoft.Extensions.Configuration;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Onetimev1;
using Descope.Auth.Models.Onetimev1;

namespace Descope.Test.Integration
{
    internal class IntegrationTestSetup
    {
        internal static string? ProjectId { get; private set; }

        internal static DescopeClientOptions GetDescopeClientOptions()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettingsTest.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            ProjectId = configuration["AppSettings:ProjectId"] ?? throw new ApplicationException("Can't run tests without a project ID");
            var managementKey = configuration["AppSettings:ManagementKey"] ?? throw new ApplicationException("Can't run tests without a management key");
            var baseUrl = configuration["AppSettings:BaseURL"];
            var isUnsafe = bool.Parse(configuration["AppSettings:Unsafe"] ?? "false");

            return new DescopeClientOptions
            {
                ProjectId = ProjectId,
                ManagementKey = managementKey,
                AuthManagementKey = managementKey,
                BaseUrl = baseUrl ?? "https://api.descope.com",
                IsUnsafe = isUnsafe,
            };
        }

        internal static IDescopeClient InitDescopeClient()
        {
            var options = GetDescopeClientOptions();
            return DescopeManagementClientFactory.Create(options);
        }

        internal static async Task<SignedInTestUser> InitTestUser(IDescopeClient descopeClient)
        {
            var loginId = Guid.NewGuid().ToString();

            // Create test user
            var createUserRequest = new CreateUserRequest
            {
                Identifier = loginId,
                Phone = "+972555555555",
                VerifiedPhone = true,
            };

            var user = await descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);

            // Generate OTP for test user
            var otpRequest = new TestUserGenerateOTPRequest
            {
                LoginId = loginId,
                DeliveryMethod = "sms"
            };

            var generatedOtp = await descopeClient.Mgmt.V1.Tests.Generate.Otp.PostAsync(otpRequest);

            // Verify OTP
            var verifyRequest = new OTPVerifyCodeRequest
            {
                LoginId = loginId,
                Code = generatedOtp?.Code ?? throw new ApplicationException("Failed to generate OTP code"),
            };

            var authInfo = await descopeClient.Auth.V1.Otp.Verify.Sms.PostAsync(verifyRequest);

            // Verify that authInfo is valid for a signed-in user
            if (authInfo == null || string.IsNullOrEmpty(authInfo.SessionJwt))
            {
                throw new ApplicationException("Failed to sign in test user");
            }
            var token = await descopeClient.Auth.ValidateSessionAsync(authInfo.SessionJwt);
            if (token == null || string.IsNullOrEmpty(token.Jwt) || token.Subject != user?.User?.UserId)
            {
                throw new ApplicationException("Failed to validate signed-in test user session");
            }

            return new SignedInTestUser(user!, authInfo!);
        }

    }

    internal class SignedInTestUser
    {
        internal UserResponse User { get; }
        internal Auth.Models.Onetimev1.JWTResponse AuthInfo { get; }

        internal SignedInTestUser(UserResponse user, Auth.Models.Onetimev1.JWTResponse authInfo)
        {
            User = user;
            AuthInfo = authInfo;
        }
    }
}
