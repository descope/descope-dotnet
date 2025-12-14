using Xunit;
using Descope.Mgmt.Models.Managementv1;
using Descope.Auth.Models.Onetimev1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class PasswordAuthenticationTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task PasswordAuthentication_SendPasswordReset()
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
                    Name = "Password Test User"
                };
                var user = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Set an initial password for the user
                var setPasswordRequest = new SetUserPasswordRequest
                {
                    Identifier = loginId,
                    Password = "initialPassword123!"
                };
                await _descopeClient.Mgmt.V1.User.Password.Set.Active.PostAsync(setPasswordRequest);

                // Send password reset email
                var passwordResetRequest = new PasswordResetSendRequest
                {
                    LoginId = loginId,
                    RedirectUrl = "https://example.com/reset-password",
                    TemplateOptions = new PasswordResetSendRequest_templateOptions
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "company", "Test Company" }
                        }
                    }
                };
                await _descopeClient.Auth.V1.Password.Reset.PostAsync(passwordResetRequest);

                // Test passes if no exception is thrown
                Assert.True(true, "Password reset email sent successfully");
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
        public async Task PasswordAuthentication_ReplaceUserPassword()
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
                    Name = "Password Test User"
                };
                var user = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Set an initial password
                var oldPassword = "oldPassword123!";
                var newPassword = "newPassword456!";
                var setPasswordRequest = new SetUserPasswordRequest
                {
                    Identifier = loginId,
                    Password = oldPassword
                };
                await _descopeClient.Mgmt.V1.User.Password.Set.Active.PostAsync(setPasswordRequest);

                // Replace the password
                var replaceRequest = new PasswordReplaceRequest
                {
                    LoginId = loginId,
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                };
                var authResponse = await _descopeClient.Auth.V1.Password.Replace.PostAsync(replaceRequest);

                // Verify the response
                Assert.NotNull(authResponse);
                Assert.NotEmpty(authResponse.SessionJwt!);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt!);
                Assert.Equal(user?.User?.UserId, authResponse.User?.UserId);
                Assert.Equal(testLoginId, authResponse.User?.Email);

                // Verify password was changed by attempting to replace it again with the new password
                var secondReplaceRequest = new PasswordReplaceRequest
                {
                    LoginId = loginId,
                    OldPassword = newPassword,
                    NewPassword = "verifyPassword789!"
                };
                var secondAuthResponse = await _descopeClient.Auth.V1.Password.Replace.PostAsync(secondReplaceRequest);
                Assert.NotNull(secondAuthResponse);
                Assert.NotEmpty(secondAuthResponse.SessionJwt!);

                // Verify old password no longer works by expecting an exception
                var invalidReplaceRequest = new PasswordReplaceRequest
                {
                    LoginId = loginId,
                    OldPassword = oldPassword,
                    NewPassword = "shouldFail123!"
                };
                async Task InvalidPasswordAttempt() => await _descopeClient.Auth.V1.Password.Replace.PostAsync(invalidReplaceRequest);
                var exception = await Assert.ThrowsAsync<DescopeException>(InvalidPasswordAttempt);
                Assert.Contains("credentials", exception.Message.ToLowerInvariant());
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
        public async Task PasswordAuthentication_UpdateUserPassword()
        {
            string? loginId = null;
            try
            {
                // Create a logged in test user
                var testUser = await IntegrationTestSetup.InitTestUser(_descopeClient);
                loginId = testUser.User.User?.LoginIds?.FirstOrDefault();

                // Set initial password for the user
                var setPasswordRequest = new SetUserPasswordRequest
                {
                    Identifier = loginId,
                    Password = "initialPassword123!"
                };
                await _descopeClient.Mgmt.V1.User.Password.Set.Active.PostAsync(setPasswordRequest);

                // Update the user's own password using their refresh JWT
                var newPassword = "userUpdatedPassword789!";
                var updateRequest = new PasswordUpdateRequest
                {
                    LoginId = loginId,
                    NewPassword = newPassword
                };
                await _descopeClient.Auth.V1.Password.Update.PostWithJwtAsync(updateRequest, testUser.AuthInfo.RefreshJwt!);

                // Verify password was changed by attempting to use the new password with ReplaceUserPassword
                var verificationReplaceRequest = new PasswordReplaceRequest
                {
                    LoginId = loginId,
                    OldPassword = newPassword,
                    NewPassword = "finalPassword999!"
                };
                var verificationAuthResponse = await _descopeClient.Auth.V1.Password.Replace.PostAsync(verificationReplaceRequest);
                Assert.NotNull(verificationAuthResponse);
                Assert.NotEmpty(verificationAuthResponse.SessionJwt!);

                // Verify old password no longer works by expecting an exception
                var invalidReplaceRequest = new PasswordReplaceRequest
                {
                    LoginId = loginId,
                    OldPassword = "initialPassword123!",
                    NewPassword = "shouldFail123!"
                };
                async Task InvalidPasswordAttempt() => await _descopeClient.Auth.V1.Password.Replace.PostAsync(invalidReplaceRequest);
                var exception = await Assert.ThrowsAsync<DescopeException>(InvalidPasswordAttempt);
                Assert.Contains("credentials", exception.Message.ToLowerInvariant());
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
        public async Task PasswordAuthentication_SendEnchantedLink()
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
                    Name = "Enchanted Link Test User"
                };
                var user = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Send enchanted link (magic link) to user via regular authentication flow
                var enchantedLinkRequest = new EnchantedLinkSignInRequest
                {
                    LoginId = testLoginId,
                    RedirectUrl = "https://example.com/auth"
                };
                var enchantedLinkResponse = await _descopeClient.Auth.V1.Enchantedlink.Signin.Email.PostAsync(enchantedLinkRequest);

                // Verify that the enchanted link was sent successfully
                Assert.NotNull(enchantedLinkResponse);
                Assert.NotEmpty(enchantedLinkResponse.PendingRef!);
                Assert.NotEmpty(enchantedLinkResponse.LinkId!);
                Assert.NotEmpty(enchantedLinkResponse.MaskedEmail!);
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
        public async Task PasswordAuthentication_ReplaceUserPassword_WithInvalidOldPassword_ShouldFail()
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
                    Name = "Password Test User"
                };
                var user = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                loginId = testLoginId;

                // Set an initial password
                var correctOldPassword = "oldPassword123!";
                var wrongOldPassword = "wrongPassword456!";
                var newPassword = "newPassword789!";
                var setPasswordRequest = new SetUserPasswordRequest
                {
                    Identifier = loginId,
                    Password = correctOldPassword
                };
                await _descopeClient.Mgmt.V1.User.Password.Set.Active.PostAsync(setPasswordRequest);

                // Try to replace password with wrong old password
                var invalidReplaceRequest = new PasswordReplaceRequest
                {
                    LoginId = loginId,
                    OldPassword = wrongOldPassword,
                    NewPassword = newPassword
                };
                async Task Act() => await _descopeClient.Auth.V1.Password.Replace.PostAsync(invalidReplaceRequest);

                // Should throw an exception for invalid old password
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("credentials", exception.Message.ToLowerInvariant());
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
