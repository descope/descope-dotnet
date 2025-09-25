using Xunit;

namespace Descope.Test.Integration
{
    public class PasswordAuthenticationTests
    {
        private readonly DescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        public PasswordAuthenticationTests()
        {
            // No cleanup needed for regular users
        }

        [Fact]
        public async Task PasswordAuthentication_SendPasswordReset()
        {
            string? loginId = null;
            try
            {
                // Create a regular user with email
                var testLoginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var user = await _descopeClient.Management.User.Create(testLoginId, new UserRequest()
                {
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "Password Test User"
                });
                loginId = testLoginId;

                // Set an initial password for the user
                await _descopeClient.Management.User.SetActivePassword(loginId, "initialPassword123!");

                // Send password reset email
                await _descopeClient.Auth.SendPasswordReset(
                    loginId,
                    "https://example.com/reset-password",
                    new Dictionary<string, string> { { "company", "Test Company" } }
                );

                // Test passes if no exception is thrown
                Assert.True(true, "Password reset email sent successfully");
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var user = await _descopeClient.Management.User.Create(testLoginId, new UserRequest()
                {
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "Password Test User"
                });
                loginId = testLoginId;

                // Set an initial password
                var oldPassword = "oldPassword123!";
                var newPassword = "newPassword456!";
                await _descopeClient.Management.User.SetActivePassword(loginId, oldPassword);

                // Replace the password
                var authResponse = await _descopeClient.Auth.ReplaceUserPassword(loginId, oldPassword, newPassword);

                // Verify the response
                Assert.NotNull(authResponse);
                Assert.NotEmpty(authResponse.SessionJwt);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt);
                Assert.Equal(user.UserId, authResponse.User.UserId);
                Assert.Equal(testLoginId, authResponse.User.Email);

                // Verify password was changed by attempting to replace it again with the new password
                var secondAuthResponse = await _descopeClient.Auth.ReplaceUserPassword(loginId, newPassword, "verifyPassword789!");
                Assert.NotNull(secondAuthResponse);
                Assert.NotEmpty(secondAuthResponse.SessionJwt);

                // Verify old password no longer works by expecting an exception
                async Task InvalidPasswordAttempt() => await _descopeClient.Auth.ReplaceUserPassword(loginId, oldPassword, "shouldFail123!");
                var exception = await Assert.ThrowsAsync<DescopeException>(InvalidPasswordAttempt);
                Assert.Contains("credentials", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                loginId = testUser.User.LoginIds.First();

                // Set initial password for the user
                await _descopeClient.Management.User.SetActivePassword(loginId, "initialPassword123!");

                // Update the user's own password using their refresh JWT
                var newPassword = "userUpdatedPassword789!";
                await _descopeClient.Auth.UpdateUserPassword(loginId, newPassword, testUser.AuthInfo.RefreshJwt!);

                // Verify password was changed by attempting to use the new password with ReplaceUserPassword
                var verificationAuthResponse = await _descopeClient.Auth.ReplaceUserPassword(loginId, newPassword, "finalPassword999!");
                Assert.NotNull(verificationAuthResponse);
                Assert.NotEmpty(verificationAuthResponse.SessionJwt);

                // Verify old password no longer works by expecting an exception
                async Task InvalidPasswordAttempt() => await _descopeClient.Auth.ReplaceUserPassword(loginId, "initialPassword123!", "shouldFail123!");
                var exception = await Assert.ThrowsAsync<DescopeException>(InvalidPasswordAttempt);
                Assert.Contains("credentials", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var user = await _descopeClient.Management.User.Create(testLoginId, new UserRequest()
                {
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "Enchanted Link Test User"
                });
                loginId = testLoginId;

                // Send enchanted link (magic link) to user via regular authentication flow
                var enchantedLinkResponse = await _descopeClient.Auth.EnchantedLink.SignIn(
                    testLoginId,
                    "https://example.com/auth"
                );

                // Verify that the enchanted link was sent successfully
                Assert.NotNull(enchantedLinkResponse);
                Assert.NotEmpty(enchantedLinkResponse.PendingRef);
                Assert.NotEmpty(enchantedLinkResponse.LinkId);
                Assert.NotEmpty(enchantedLinkResponse.MaskedEmail);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
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
                var user = await _descopeClient.Management.User.Create(testLoginId, new UserRequest()
                {
                    Email = testLoginId,
                    VerifiedEmail = true,
                    Name = "Password Test User"
                });
                loginId = testLoginId;

                // Set an initial password
                var correctOldPassword = "oldPassword123!";
                var wrongOldPassword = "wrongPassword456!";
                var newPassword = "newPassword789!";
                await _descopeClient.Management.User.SetActivePassword(loginId, correctOldPassword);

                // Try to replace password with wrong old password
                async Task Act() => await _descopeClient.Auth.ReplaceUserPassword(loginId, wrongOldPassword, newPassword);

                // Should throw an exception for invalid old password
                var exception = await Assert.ThrowsAsync<DescopeException>(Act);
                Assert.Contains("credentials", exception.Message.ToLowerInvariant());
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await _descopeClient.Management.User.Delete(loginId); }
                    catch { }
                }
            }
        }
    }
}
