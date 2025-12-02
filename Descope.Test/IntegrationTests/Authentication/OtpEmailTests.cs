using Xunit;
using Descope.Auth.Models.Onetimev1;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Onetimev1;

namespace Descope.Test.Integration
{
    public class OtpEmailTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task OtpEmail_SignInAndVerify_Success()
        {
            string? loginId = null;
            try
            {
                // Create a test user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "OTP Email Test User"
                };

                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Generate OTP for test user
                var otpRequest = new TestUserGenerateOTPRequest
                {
                    DeliveryMethod = "email",
                    LoginId = testLoginId
                };

                var otpResponse = await _descopeClient.Mgmt.V1.Tests.Generate.Otp.PostAsync(otpRequest);

                Assert.NotNull(otpResponse);
                Assert.NotNull(otpResponse.Code);
                Assert.NotEmpty(otpResponse.Code);

                // Sign in using OTP email
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Verify the OTP code
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = testLoginId,
                    Code = otpResponse.Code
                };

                var authResponse = await _descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(verifyRequest);

                // Verify the response
                Assert.NotNull(authResponse);
                Assert.NotEmpty(authResponse.SessionJwt!);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt);
                Assert.Equal(user!.User!.UserId, authResponse.User?.UserId);
                Assert.Equal(testLoginId, authResponse.User?.Email);

                // Validate the session JWT
                var validatedToken = await _descopeClient.Auth.ValidateSessionAsync(authResponse.SessionJwt!);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpEmail_Verify_WithInvalidCode_ShouldFail()
        {
            string? loginId = null;
            try
            {
                // Create a test user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "OTP Email Invalid Code Test User"
                };

                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Sign in using OTP email
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Try to verify with an invalid OTP code
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = testLoginId,
                    Code = "123456" // Invalid code
                };

                async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(verifyRequest);

                // Should throw an exception for invalid code
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("code", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpEmail_Verify_WithEmptyCode_ShouldFail()
        {
            string? loginId = null;
            try
            {
                // Create a test user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "OTP Email Empty Code Test User"
                };

                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Sign in using OTP email
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Try to verify with an empty OTP code
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = testLoginId,
                    Code = ""
                };

                async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(verifyRequest);

                // Should throw an exception for empty code
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("code", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpEmail_Verify_WithNullCode_ShouldFail()
        {
            string? loginId = null;
            try
            {
                // Create a test user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "OTP Email Null Code Test User"
                };

                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Sign in using OTP email
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Try to verify with a null OTP code
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = testLoginId,
                    Code = null
                };

                async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(verifyRequest);

                // Should throw an exception for null code
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("code", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpEmail_SignIn_WithNonExistentUser_ShouldFail()
        {
            // Try to sign in with a non-existent user
            var nonExistentEmail = Guid.NewGuid().ToString() + "@nonexistent.descope.com";
            var signInRequest = new OTPSignInRequest
            {
                LoginId = nonExistentEmail
            };

            async Task Act() => await _descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(signInRequest);

            // Should throw an exception for non-existent user
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.Contains("user", exception.Message.ToLowerInvariant());
        }

        [Fact]
        public async Task OtpEmail_Verify_WithNonExistentUser_ShouldFail()
        {
            // Try to verify OTP for a non-existent user
            var nonExistentEmail = Guid.NewGuid().ToString() + "@nonexistent.descope.com";
            var verifyRequest = new OTPVerifyCodeRequest
            {
                LoginId = nonExistentEmail,
                Code = "123456"
            };

            async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(verifyRequest);

            // Should throw an exception for non-existent user or expired code
            var exception = await Assert.ThrowsAsync<DescopeException>(Act);
            Assert.True(exception.Message.ToLowerInvariant().Contains("user") || exception.Message.ToLowerInvariant().Contains("code") || exception.Message.ToLowerInvariant().Contains("expired"));
        }

        [Fact]
        public async Task OtpEmail_Verify_WithWrongLoginId_ShouldFail()
        {
            string? loginId = null;
            try
            {
                // Create a test user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "OTP Email Wrong LoginId Test User"
                };

                var user = await _descopeClient.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Generate OTP for test user
                var otpRequest = new TestUserGenerateOTPRequest
                {
                    DeliveryMethod = "email",
                    LoginId = testLoginId
                };

                var otpResponse = await _descopeClient.Mgmt.V1.Tests.Generate.Otp.PostAsync(otpRequest);

                Assert.NotNull(otpResponse);
                Assert.NotNull(otpResponse.Code);

                // Sign in using OTP email
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Try to verify with a different loginId
                var wrongLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var verifyRequest = new OTPVerifyCodeRequest
                {
                    LoginId = wrongLoginId,
                    Code = otpResponse.Code
                };

                async Task Act() => await _descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(verifyRequest);

                // Should throw an exception for wrong loginId
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.True(exception.Message.ToLowerInvariant().Contains("user") || exception.Message.ToLowerInvariant().Contains("code"));
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task OtpEmail_SignInRegularUser_GeneratesOtpSuccessfully()
        {
            string? loginId = null;
            try
            {
                // Create a regular user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = testLoginId,
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "OTP Email Generation Test User"
                };

                var user = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Call the actual signin method (non-test) which generates and sends OTP
                // We just verify it returns successfully (200 OK)
                var signInRequest = new OTPSignInRequest
                {
                    LoginId = testLoginId
                };

                var signInResponse = await _descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(signInRequest);

                // Verify the response is not null (indicates successful OTP generation)
                Assert.NotNull(signInResponse);
                // Note: We don't verify the OTP code here since we can't receive the actual email
                // This test just confirms the OTP generation endpoint works correctly
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }
    }
}
