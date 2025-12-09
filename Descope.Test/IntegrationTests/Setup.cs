using Descope;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Onetimev1;
using Descope.Auth.Models.Onetimev1;

namespace Descope.Test.Integration
{
    internal class IntegrationTestSetup
    {
        internal static string? ProjectId { get; private set; }
        internal static IDescopeClient InitDescopeClient()
        {
            // Read configuration from appsettingsTest.json
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettingsTest.json");
            var json = File.ReadAllText(configPath);
            var config = System.Text.Json.JsonDocument.Parse(json);
            var appSettings = config.RootElement.GetProperty("AppSettings");

            ProjectId = appSettings.GetProperty("ProjectId").GetString() ?? throw new ApplicationException("Can't run tests without a project ID");
            var managementKey = appSettings.GetProperty("ManagementKey").GetString() ?? throw new ApplicationException("Can't run tests without a management key");
            var baseUrl = appSettings.TryGetProperty("BaseURL", out var baseUrlElement) ? baseUrlElement.GetString() : null;
            var isUnsafe = appSettings.TryGetProperty("Unsafe", out var unsafeElement) && bool.Parse(unsafeElement.GetString() ?? "false");

            var options = new DescopeClientOptions
            {
                ProjectId = ProjectId,
                ManagementKey = managementKey,
                AuthManagementKey = managementKey,
                BaseUrl = baseUrl ?? "https://api.descope.com",
                IsUnsafe = isUnsafe,
            };

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
