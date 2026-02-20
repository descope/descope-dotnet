using Xunit;
using Xunit.Abstractions;
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
        private readonly ITestOutputHelper _output;

        public ServiceDITests(ITestOutputHelper output)
        {
            _output = output;
        }

        private IDescopeClient InitDescopeClientWithDI(HttpLoggingHandler? loggingHandler = null)
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
            var httpClientBuilder = services.AddHttpClient("DescopeClient");

            // Add HTTP logging handler if provided (outermost - sees all requests/responses)
            if (loggingHandler != null)
            {
                httpClientBuilder.AddHttpMessageHandler(() => loggingHandler);
            }

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

        [Fact]
        public async Task ServiceDI_MgmtCallAfterAuthVerify_ShouldNotLeakCookies()
        {
            var httpLogger = new HttpLoggingHandler(_output);
            var client = InitDescopeClientWithDI(httpLogger);
            string? loginId = null;

            try
            {
                // 1. Create test user (mgmt call)
                loginId = Guid.NewGuid().ToString() + "@test.descope.com";
                _output?.WriteLine($"=== Step 1: Creating test user {loginId} ===");
                await client.Mgmt.V1.User.Create.Test.PostAsync(new CreateUserRequest
                {
                    Identifier = loginId,
                    Email = loginId,
                    VerifiedEmail = true,
                    Name = "Cookie Leak Test User"
                });

                // 2. Generate OTP (mgmt call)
                _output?.WriteLine("=== Step 2: Generating OTP ===");
                var otpResponse = await client.Mgmt.V1.Tests.Generate.Otp.PostAsync(
                    new TestUserGenerateOTPRequest { LoginId = loginId, DeliveryMethod = "email" });

                // 3. Verify OTP (auth call) - sets cookies in "Manage in cookies" mode
                _output?.WriteLine("=== Step 3: Verifying OTP (auth call - should trigger Set-Cookie in cookies mode) ===");
                var authResponse = await client.Auth.V1.Otp.Verify.Email.PostAsync(
                    new Auth.Models.Onetimev1.OTPVerifyCodeRequest { LoginId = loginId, Code = otpResponse!.Code });
                Assert.NotNull(authResponse);
                _output?.WriteLine($"  Auth response sessionJwt present: {!string.IsNullOrEmpty(authResponse.SessionJwt)}");
                _output?.WriteLine($"  Auth response refreshJwt present: {!string.IsNullOrEmpty(authResponse.RefreshJwt)}");

                // 4. Management call AFTER auth - should succeed without cookie contamination
                _output?.WriteLine("=== Step 4: Management call AFTER auth (should not have leaked cookies) ===");
                var searchResponse = await client.Mgmt.V2.User.Search.PostAsync(
                    new SearchUsersRequest { Limit = 1 });
                Assert.NotNull(searchResponse);
                _output?.WriteLine("=== All steps passed ===");
            }
            finally
            {
                if (!string.IsNullOrEmpty(loginId))
                {
                    try { await client.Mgmt.V1.User.DeletePath.PostAsync(
                        new DeleteUserRequest { Identifier = loginId }); }
                    catch { }
                }
            }
        }

        /// <summary>
        /// DelegatingHandler that logs raw HTTP request/response details for debugging.
        /// Logs method, URL, request headers, status code, response headers, and response body.
        /// </summary>
        private class HttpLoggingHandler : DelegatingHandler
        {
            private readonly ITestOutputHelper? _output;
            private int _requestCount;

            public HttpLoggingHandler(ITestOutputHelper? output)
            {
                _output = output;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var requestNum = Interlocked.Increment(ref _requestCount);
                _output?.WriteLine($"--- HTTP Request #{requestNum} ---");
                _output?.WriteLine($"  {request.Method} {request.RequestUri}");
                foreach (var header in request.Headers)
                {
                    _output?.WriteLine($"  > {header.Key}: {string.Join(", ", header.Value)}");
                }
                if (request.Content != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        _output?.WriteLine($"  > {header.Key}: {string.Join(", ", header.Value)}");
                    }
                }

                var response = await base.SendAsync(request, cancellationToken);

                _output?.WriteLine($"--- HTTP Response #{requestNum} ({(int)response.StatusCode} {response.StatusCode}) ---");
                foreach (var header in response.Headers)
                {
                    _output?.WriteLine($"  < {header.Key}: {string.Join(", ", header.Value)}");
                }
                if (response.Content != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        _output?.WriteLine($"  < {header.Key}: {string.Join(", ", header.Value)}");
                    }
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    // Truncate very long bodies (JWTs etc.) for readability
                    var displayBody = body.Length > 500 ? body.Substring(0, 500) + "... [truncated]" : body;
                    _output?.WriteLine($"  < Body: {displayBody}");
                    // Re-create content since ReadAsStringAsync consumed it
                    response.Content = new StringContent(body, System.Text.Encoding.UTF8,
                        response.Content.Headers.ContentType?.MediaType ?? "application/json");
                }

                return response;
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
