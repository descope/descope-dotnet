using Xunit;
using Descope.Auth.Models.Onetimev1;
using Descope.Mgmt.Models.Managementv1;
using Descope.Mgmt.Models.Onetimev1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class ServiceDITests : RateLimitedIntegrationTest
    {
        private IDescopeClient InitDescopeClientWithDI()
        {
            var options = IntegrationTestSetup.GetDescopeClientOptions();

            // Configure services with DI container
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Configure HttpClient with HttpClientFactory
            services.AddHttpClient("DescopeClient");

            // Register Descope Client using the extension method
            options.HttpClientFactoryName = "DescopeClient";
            services.AddDescopeClient(options);

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetRequiredService<IDescopeClient>();
        }

        [Fact]
        public async Task ServiceDI_CreateAndSearchUser_Success()
        {
            var client = InitDescopeClientWithDI();
            string? loginId = null;

            try
            {
                // Create a test user
                loginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = loginId,
                    Email = loginId,
                    VerifiedEmail = true,
                    Name = "Service DI Test User"
                };

                var user = await client.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);

                Assert.NotNull(user);
                Assert.NotNull(user.User);
                Assert.Equal(loginId, user.User.Email);

                // Search for users
                var searchResponse = await client.Mgmt.V2.User.Search.PostAsync(
                    new SearchUsersRequest
                    {
                        Limit = 5
                    });

                Assert.NotNull(searchResponse);
                Assert.True(searchResponse.Total > 0);
                Assert.NotNull(searchResponse.Users);
                Assert.NotEmpty(searchResponse.Users);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await client.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task ServiceDI_MagicLinkFlow_Success()
        {
            var client = InitDescopeClientWithDI();
            string? loginId = null;

            try
            {
                // Create a test user
                loginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = loginId,
                    Email = loginId,
                    VerifiedEmail = true,
                    Name = "Service DI Magic Link Test User"
                };

                var user = await client.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);

                Assert.NotNull(user);
                Assert.NotNull(user.User);

                // Generate magic link for test user with custom claims
                var loginOptions = new Descope.Mgmt.Models.Onetimev1.LoginOptions
                {
                    CustomClaims = new Descope.Mgmt.Models.Onetimev1.LoginOptions_customClaims
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "testKey", "testValue" },
                            { "numericKey", 42 }
                        }
                    }
                };

                var magicLinkResponse = await client.Mgmt.V1.Tests.Generate.Magiclink.PostAsync(
                    new TestUserGenerateMagicLinkRequest
                    {
                        LoginId = loginId,
                        DeliveryMethod = "email",
                        RedirectUrl = "https://example.com/auth",
                        LoginOptions = loginOptions
                    });

                Assert.NotNull(magicLinkResponse);
                Assert.NotNull(magicLinkResponse.Link);
                Assert.NotEmpty(magicLinkResponse.Link);

                // Extract token from the magic link URL
                var uri = new Uri(magicLinkResponse.Link);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var token = queryParams["t"];

                Assert.NotNull(token);
                Assert.NotEmpty(token);

                // Verify the magic link token
                var authResponse = await client.Auth.V1.Magiclink.Verify.PostAsync(
                    new VerifyMagicLinkRequest
                    {
                        Token = token
                    });

                Assert.NotNull(authResponse);
                Assert.NotNull(authResponse.SessionJwt);
                Assert.NotEmpty(authResponse.SessionJwt);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt);
                Assert.Equal(user.User.UserId, authResponse.User?.UserId);

                // Validate the session JWT locally (no HTTP call)
                var validatedToken = await client.Auth.ValidateSessionAsync(authResponse.SessionJwt);

                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);
                Assert.Equal(user.User.UserId, validatedToken.Subject);
                Assert.NotNull(validatedToken.Claims);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await client.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task ServiceDI_OtpEmailFlow_Success()
        {
            var client = InitDescopeClientWithDI();
            string? loginId = null;

            try
            {
                // Create a test user with email
                loginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = loginId,
                    Email = loginId,
                    VerifiedEmail = true,
                    Name = "Service DI OTP Email Test User"
                };

                var user = await client.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);

                Assert.NotNull(user);
                Assert.NotNull(user.User);

                // Generate OTP for test user
                var otpRequest = new TestUserGenerateOTPRequest
                {
                    DeliveryMethod = "email",
                    LoginId = loginId
                };

                var otpResponse = await client.Mgmt.V1.Tests.Generate.Otp.PostAsync(otpRequest);

                Assert.NotNull(otpResponse);
                Assert.NotNull(otpResponse.Code);
                Assert.NotEmpty(otpResponse.Code);

                // Sign in using OTP email
                var signInRequest = new Auth.Models.Onetimev1.OTPSignInRequest
                {
                    LoginId = loginId
                };

                var signInResponse = await client.Auth.V1.Otp.Signin.Email.PostAsync(signInRequest);

                Assert.NotNull(signInResponse);

                // Verify the OTP code
                var verifyRequest = new Auth.Models.Onetimev1.OTPVerifyCodeRequest
                {
                    LoginId = loginId,
                    Code = otpResponse.Code
                };

                var authResponse = await client.Auth.V1.Otp.Verify.Email.PostAsync(verifyRequest);

                Assert.NotNull(authResponse);
                Assert.NotEmpty(authResponse.SessionJwt!);
                Assert.NotNull(authResponse.RefreshJwt);
                Assert.NotEmpty(authResponse.RefreshJwt);
                Assert.Equal(user.User.UserId, authResponse.User?.UserId);
                Assert.Equal(loginId, authResponse.User?.Email);

                // Validate the session JWT
                var validatedToken = await client.Auth.ValidateSessionAsync(authResponse.SessionJwt!);
                Assert.NotNull(validatedToken);
                Assert.Equal(authResponse.SessionJwt, validatedToken.Jwt);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await client.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task ServiceDI_UpdateEmailUsingJwt_Success()
        {
            var client = InitDescopeClientWithDI();
            string? loginId = null;

            try
            {
                // Create a test user
                loginId = Guid.NewGuid().ToString() + "@test.descope.com";
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = loginId,
                    Email = loginId,
                    VerifiedEmail = true,
                    Name = "Service DI Update Email Test User"
                };

                var user = await client.Mgmt.V1.User.Create.Test.PostAsync(createUserRequest);

                Assert.NotNull(user);
                Assert.NotNull(user.User);

                // Generate magic link to get JWT
                var magicLinkResponse = await client.Mgmt.V1.Tests.Generate.Magiclink.PostAsync(
                    new TestUserGenerateMagicLinkRequest
                    {
                        LoginId = loginId,
                        DeliveryMethod = "email",
                        RedirectUrl = "https://example.com/auth"
                    });

                var uri = new Uri(magicLinkResponse!.Link!);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var token = queryParams["t"];

                var authResponse = await client.Auth.V1.Magiclink.Verify.PostAsync(
                    new VerifyMagicLinkRequest
                    {
                        Token = token
                    });

                Assert.NotNull(authResponse);
                Assert.NotNull(authResponse.RefreshJwt);

                // Update user email using Auth V1 API with JWT
                var newEmail = "updated_" + loginId;
                var updateEmailResponse = await client.Auth.V1.Magiclink.Update.Email.PostWithJwtAsync(
                    new UpdateUserEmailMagicLinkRequest
                    {
                        LoginId = loginId,
                        Email = newEmail,
                        RedirectUrl = "https://example.com/email-updated",
                        AddToLoginIDs = true,
                        OnMergeUseExisting = true,
                    },
                    authResponse.RefreshJwt);

                Assert.NotNull(updateEmailResponse);
                Assert.NotNull(updateEmailResponse.MaskedEmail);
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await client.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        // This test relies on the FixRootResponseBodyHandler middleware to correctly parse the response for loading a tenant, makes sure it applies to DI setup as well
        [Fact]
        public async Task ServiceDI_CreateAndLoadTenant_Success()
        {
            var client = InitDescopeClientWithDI();
            string? tenantId = null;

            try
            {
                // Create a test tenant
                var tenantName = Guid.NewGuid().ToString();
                var createTenantRequest = new CreateTenantRequest
                {
                    Name = tenantName
                };

                var createResponse = await client.Mgmt.V1.Tenant.Create.PostAsync(createTenantRequest);

                Assert.NotNull(createResponse);
                Assert.NotNull(createResponse.Id);
                tenantId = createResponse.Id;

                // Load the tenant using GetWithIdAsync
                var loadResponse = await client.Mgmt.V1.Tenant.GetWithIdAsync(tenantId);

                Assert.NotNull(loadResponse);
                Assert.NotNull(loadResponse.Tenant);
                Assert.Equal(tenantId, loadResponse.Tenant.Id);
                Assert.Equal(tenantName, loadResponse.Tenant.Name);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    try { await client.Mgmt.V1.Tenant.DeletePath.PostAsync(new DeleteTenantRequest { Id = tenantId }); }
                    catch { }
                }
            }
        }
    }
}
