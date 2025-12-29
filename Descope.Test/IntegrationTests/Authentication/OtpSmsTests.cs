using Xunit;
using Descope.Auth.Models.Onetimev1;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Onetimev1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class OtpSmsTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();
        // The tests in this class perform many user mgmt calls, which have lower rate limits, so we add extra delay in addition to the base rate limiting

        [Fact]
        public async Task OtpSms_SignInAndVerify_Success()
        {
            string? loginId = null;
            try
            {
                // Create a test user with phone
                var testLoginId = Guid.NewGuid().ToString();
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                    Name = "OTP SMS Test User"
                };

                await Task.Delay(extraSleepTime);
                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Generate OTP for test user
                var otpRequest = new TestUserGenerateOTPRequest
                {
                    DeliveryMethod = "sms",
                    LoginId = testLoginId
                };

                var otpResponse = await _descopeClient.Mgmt.V1.Tests.Generate.Otp.PostAsync(otpRequest);

                Assert.NotNull(otpResponse);
                Assert.NotNull(otpResponse.Code);
                Assert.NotEmpty(otpResponse.Code);

                // Sign in using OTP SMS
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                await Task.Delay(extraSleepTime);
                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Sms.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Verify the OTP code
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = testLoginId,
                    Code = otpResponse.Code
                };

                var authResponse = await _descopeClient.Auth.V1.Otp.Verify.Sms.PostAsync(verifyRequest);

                // Verify the response
                Assert.NotNull(authResponse);
                Assert.NotEmpty(authResponse.SessionJwt!);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt);
                Assert.Equal(user!.User!.UserId, authResponse.User?.UserId);
                Assert.Equal(testLoginId, authResponse.User?.LoginIds?[0]);

                // Validate the session JWT
                var validatedToken = await _descopeClient.Auth.ValidateSessionAsync(authResponse.SessionJwt!);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await Task.Delay(extraSleepTime); await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpSms_Verify_WithInvalidCode_ShouldFail()
        {
            string? loginId = null;
            try
            {
                // Create a test user with phone
                var testLoginId = Guid.NewGuid().ToString();
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                    Name = "OTP SMS Invalid Code Test User"
                };

                await Task.Delay(extraSleepTime);
                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Sign in using OTP SMS
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                await Task.Delay(extraSleepTime);
                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Sms.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Try to verify with an invalid OTP code
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = testLoginId,
                    Code = "123456" // Invalid code
                };

                async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Sms.PostAsync(verifyRequest);

                // Should throw an exception for invalid code
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("code", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await Task.Delay(extraSleepTime); await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpSms_Verify_WithEmptyCode_ShouldFail()
        {
            string? loginId = null;
            try
            {
                // Create a test user with phone
                var testLoginId = Guid.NewGuid().ToString();
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                    Name = "OTP SMS Empty Code Test User"
                };

                await Task.Delay(extraSleepTime);
                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Sign in using OTP SMS
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                await Task.Delay(extraSleepTime);
                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Sms.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Try to verify with an empty OTP code
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = testLoginId,
                    Code = ""
                };

                async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Sms.PostAsync(verifyRequest);

                // Should throw an exception for empty code
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("code", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await Task.Delay(extraSleepTime); await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpSms_Verify_WithNullCode_ShouldFail()
        {
            string? loginId = null;
            try
            {
                // Create a test user with phone
                var testLoginId = Guid.NewGuid().ToString();
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                    Name = "OTP SMS Null Code Test User"
                };

                await Task.Delay(extraSleepTime);
                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Sign in using OTP SMS
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                await Task.Delay(extraSleepTime);
                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Sms.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Try to verify with a null OTP code
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = testLoginId,
                    Code = null
                };

                async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Sms.PostAsync(verifyRequest);

                // Should throw an exception for null code
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("code", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await Task.Delay(extraSleepTime); await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpSms_SignIn_WithNonExistentUser_ShouldFail()
        {
            // Try to sign in with a non-existent user
            var nonExistentLoginId = Guid.NewGuid().ToString();
            var signInRequest = new OTPSignInRequest
            {
                LoginId = nonExistentLoginId
            };

            await Task.Delay(extraSleepTime);
            async Task Act() => await _descopeClient.Auth.V1.Otp.Signin.Sms.PostAsync(signInRequest);

            // Should throw an exception for non-existent user
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("user", exception.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task OtpSms_Verify_WithNonExistentUser_ShouldFail()
        {
            // Try to verify OTP for a non-existent user
            var nonExistentLoginId = Guid.NewGuid().ToString();
            var verifyRequest = new OTPVerifyCodeRequest
            {
                LoginId = nonExistentLoginId,
                Code = "123456"
            };

            async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Sms.PostAsync(verifyRequest);

            // Should throw an exception for non-existent user or expired code
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.True(exception.Message.ToLowerInvariant().Contains("user") || exception.Message.ToLowerInvariant().Contains("code") || exception.Message.ToLowerInvariant().Contains("expired"));
        }

        [Fact]
        public async Task OtpSms_Verify_WithWrongLoginId_ShouldFail()
        {
            string? loginId = null;
            try
            {
                // Create a test user with phone
                var testLoginId = Guid.NewGuid().ToString();
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                    Name = "OTP SMS Wrong LoginId Test User"
                };

                await Task.Delay(extraSleepTime);
                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Generate OTP for test user
                var otpRequest = new TestUserGenerateOTPRequest
                {
                    DeliveryMethod = "sms",
                    LoginId = testLoginId
                };

                var otpResponse = await _descopeClient.Mgmt.V1.Tests.Generate.Otp.PostAsync(otpRequest);

                Assert.NotNull(otpResponse);
                Assert.NotNull(otpResponse.Code);

                // Sign in using OTP SMS
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                await Task.Delay(extraSleepTime);
                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Sms.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Try to verify with a different loginId
                var wrongLoginId = Guid.NewGuid().ToString();
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = wrongLoginId,
                    Code = otpResponse.Code
                };

                async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Sms.PostAsync(verifyRequest);

                // Should throw an exception for wrong loginId
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.True(exception.Message.ToLowerInvariant().Contains("user") || exception.Message.ToLowerInvariant().Contains("code"));
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await Task.Delay(extraSleepTime); await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact(Skip = "Hits rate limit in CI/CD pipelines due to real user sms sending")]
        public async Task OtpSms_SignInRegularUser_GeneratesOtpSuccessfully()
        {
            string? loginId = null;
            try
            {
                // Create a regular user with phone
                var testLoginId = Guid.NewGuid().ToString();
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Phone = "+972555555555",
                    VerifiedPhone = true,
                    Name = "OTP SMS Generation Test User"
                };

                await Task.Delay(extraSleepTime);
                var user = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Call the actual signin method (non-test) which generates and sends OTP
                // We just verify it returns successfully (200 OK)
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                await Task.Delay(extraSleepTime);
                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Sms.PostAsync(signInRequest);

                // Verify the response is not null (indicates successful OTP generation)
                Assert.NotNull(signInResponse);
                // Note: We don't verify the OTP code here since we can't receive the actual SMS
                // This test just confirms the OTP generation endpoint works correctly
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await Task.Delay(extraSleepTime); await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }
    }
}
