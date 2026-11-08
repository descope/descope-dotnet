using Descope.Auth.Models.Onetimev1;
using Descope.Auth.Models.Userv1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Unit tests for OTP authentication using the Kiota-based DescopeClient.
/// These tests use a mock request adapter to simulate API responses without making actual HTTP calls.
/// </summary>
public class OtpTests
{
    private const string TestRefreshJwt = "test_refresh_jwt";
    private const string TestLoginId = "test-login-id";
    private const string TestEmail = "test@example.com";
    private const string TestPhone = "+11234567890";
    private const string TestTenantId = "test-tenant-id";

    [Fact]
    public async Task Otp_SignInEmail_SendsUseRecoveryAndTenantId()
    {
        OTPSignInRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignInRequest, EmailOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signin/email");
            capturedRequestBody = requestBody;
            return new EmailOperationResponse { MaskedEmail = "t***@example.com" };
        });

        var response = await descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(new OTPSignInRequest
        {
            LoginId = TestLoginId,
            UseRecovery = true,
            LoginOptions = new LoginOptions { TenantId = TestTenantId },
        });

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.LoginId.Should().Be(TestLoginId);
        capturedRequestBody.UseRecovery.Should().BeTrue();
        capturedRequestBody.LoginOptions!.TenantId.Should().Be(TestTenantId);
        response!.MaskedEmail.Should().Be("t***@example.com");
    }

    [Fact]
    public async Task Otp_SignUpInSms_SendsUseRecoveryAndTenantId()
    {
        OTPSignInRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignInRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signup-in/sms");
            capturedRequestBody = requestBody;
            return new PhoneOperationResponse { MaskedPhone = "+1*******890" };
        });

        var response = await descopeClient.Auth.V1.Otp.SignupIn.Sms.PostAsync(new OTPSignInRequest
        {
            LoginId = TestLoginId,
            UseRecovery = false,
            LoginOptions = new LoginOptions { TenantId = TestTenantId },
        });

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.UseRecovery.Should().BeFalse();
        capturedRequestBody.LoginOptions!.TenantId.Should().Be(TestTenantId);
        response!.MaskedPhone.Should().Be("+1*******890");
    }

    [Fact]
    public async Task Otp_SignInSms_OmitsUnsetNewFields()
    {
        OTPSignInRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignInRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            capturedRequestBody = requestBody;
            return new PhoneOperationResponse();
        });

        await descopeClient.Auth.V1.Otp.Signin.Sms.PostAsync(new OTPSignInRequest { LoginId = TestLoginId, LoginOptions = new LoginOptions() });

        capturedRequestBody!.UseRecovery.Should().BeNull();
        capturedRequestBody.LoginOptions!.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Otp_SignUpEmail_SendsLoginOptionsTenantId()
    {
        OTPSignUpEmailRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignUpEmailRequest, EmailOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signup/email");
            capturedRequestBody = requestBody;
            return new EmailOperationResponse { MaskedEmail = "t***@example.com" };
        });

        var response = await descopeClient.Auth.V1.Otp.Signup.Email.PostAsync(new OTPSignUpEmailRequest
        {
            LoginId = TestLoginId,
            Email = TestEmail,
            LoginOptions = new SignupLoginOptions { TenantId = TestTenantId },
        });

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.Email.Should().Be(TestEmail);
        capturedRequestBody.LoginOptions!.TenantId.Should().Be(TestTenantId);
        response!.MaskedEmail.Should().Be("t***@example.com");
    }

    [Fact]
    public async Task Otp_SignUpVoice_SendsLoginOptionsTenantId()
    {
        OTPSignUpPhoneRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignUpPhoneRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signup/voice");
            capturedRequestBody = requestBody;
            return new PhoneOperationResponse { MaskedPhone = "+1*******890" };
        });

        await descopeClient.Auth.V1.Otp.Signup.Voice.PostAsync(new OTPSignUpPhoneRequest
        {
            LoginId = TestLoginId,
            Phone = TestPhone,
            LoginOptions = new SignupLoginOptions { TenantId = TestTenantId },
        });

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.Phone.Should().Be(TestPhone);
        capturedRequestBody.LoginOptions!.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public async Task Otp_VerifyEmail_ReturnsNewResponseFields()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPVerifyCodeRequest, JWTResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/verify/email");
            requestBody!.Code.Should().Be("123456");
            return new JWTResponse
            {
                SessionJwt = "session_jwt",
                RefreshJwt = "refresh_jwt",
                TrustedDeviceJwt = "trusted_device_jwt",
                KnownDeviceJwt = "known_device_jwt",
                NextRefreshSeconds = 60,
                CookieName = "DSR",
                SessionCookieName = "DS",
                SessionCookieDomain = "example.com",
                UnsavedSSO = true,
                SsoAuthenticationOnly = false,
                UserCreationDeferred = true,
                IdpResponse = new IDPResponse { IdpOIDCClaims = new IDPResponse_idpOIDCClaims { AdditionalData = { ["sub"] = "subject" } } },
                User = new ResponseUser
                {
                    UserId = "user-id",
                    RoleIds = new List<string> { "role-id" },
                    UserTenants = new List<UserTenants> { new() { TenantId = TestTenantId, RoleIds = new List<string> { "tenant-role-id" } } },
                    ConsentExpiration = 1,
                    RecoveryEmail = "recovery@example.com",
                    VerifiedRecoveryEmail = true,
                    RecoveryPhone = "+10000000000",
                    VerifiedRecoveryPhone = false,
                    ModifiedTime = 2,
                    RecoveryCodes = true,
                    Dependent = false,
                    UserFamilies = new List<UserFamilies> { new() { FamilyId = "family-id", RoleNames = new List<string> { "member" } } },
                    LockReason = "locked",
                    TempLockExpiration = 3,
                    ScimTenantId = "scim-tenant",
                    SamlTenantId = "saml-tenant",
                    OidcTenantId = "oidc-tenant",
                    ConsentTenantId = "consent-tenant",
                    LastLoginTime = 4,
                },
            };
        });

        var response = await descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(new OTPVerifyCodeRequest { LoginId = TestLoginId, Code = "123456" });

        response.Should().NotBeNull();
        response!.TrustedDeviceJwt.Should().Be("trusted_device_jwt");
        response.KnownDeviceJwt.Should().Be("known_device_jwt");
        response.NextRefreshSeconds.Should().Be(60);
        response.CookieName.Should().Be("DSR");
        response.SessionCookieName.Should().Be("DS");
        response.SessionCookieDomain.Should().Be("example.com");
        response.UnsavedSSO.Should().BeTrue();
        response.SsoAuthenticationOnly.Should().BeFalse();
        response.UserCreationDeferred.Should().BeTrue();
        response.IdpResponse!.IdpOIDCClaims!.AdditionalData["sub"].Should().Be("subject");
        var user = response.User!;
        user.RoleIds.Should().Equal("role-id");
        user.UserTenants![0].RoleIds.Should().Equal("tenant-role-id");
        user.ConsentExpiration.Should().Be(1);
        user.RecoveryEmail.Should().Be("recovery@example.com");
        user.VerifiedRecoveryEmail.Should().BeTrue();
        user.RecoveryPhone.Should().Be("+10000000000");
        user.VerifiedRecoveryPhone.Should().BeFalse();
        user.ModifiedTime.Should().Be(2);
        user.RecoveryCodes.Should().BeTrue();
        user.Dependent.Should().BeFalse();
        user.UserFamilies![0].FamilyId.Should().Be("family-id");
        user.UserFamilies[0].RoleNames.Should().Equal("member");
        user.LockReason.Should().Be("locked");
        user.TempLockExpiration.Should().Be(3);
        user.ScimTenantId.Should().Be("scim-tenant");
        user.SamlTenantId.Should().Be("saml-tenant");
        user.OidcTenantId.Should().Be("oidc-tenant");
        user.ConsentTenantId.Should().Be("consent-tenant");
        user.LastLoginTime.Should().Be(4);
    }

    [Fact]
    public async Task Otp_UpdateEmail_PostWithJwt_SendsFailOnConflictAndMfa()
    {
        UpdateUserEmailOTPRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UpdateUserEmailOTPRequest, EmailOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/update/email");
            var jwtOption = requestInfo.RequestOptions.OfType<DescopeJwtOption>().SingleOrDefault();
            jwtOption.Should().NotBeNull();
            jwtOption!.GetContext()["jwt"].Should().Be(TestRefreshJwt);
            capturedRequestBody = requestBody;
            return new EmailOperationResponse { MaskedEmail = "t***@example.com" };
        });

        var response = await descopeClient.Auth.V1.Otp.Update.Email.PostWithJwtAsync(new UpdateUserEmailOTPRequest
        {
            LoginId = TestLoginId,
            Email = TestEmail,
            FailOnConflict = true,
            Mfa = true,
        }, TestRefreshJwt);

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.FailOnConflict.Should().BeTrue();
        capturedRequestBody.Mfa.Should().BeTrue();
        response!.MaskedEmail.Should().Be("t***@example.com");
    }

    [Fact]
    public async Task Otp_UpdatePhoneSms_PostWithJwt_SendsFailOnConflictAndMfa()
    {
        UpdateUserPhoneOTPRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UpdateUserPhoneOTPRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/update/phone/sms");
            var jwtOption = requestInfo.RequestOptions.OfType<DescopeJwtOption>().SingleOrDefault();
            jwtOption.Should().NotBeNull();
            jwtOption!.GetContext()["jwt"].Should().Be(TestRefreshJwt);
            capturedRequestBody = requestBody;
            return new PhoneOperationResponse { MaskedPhone = "+1*******890" };
        });

        await descopeClient.Auth.V1.Otp.Update.Phone.Sms.PostWithJwtAsync(new UpdateUserPhoneOTPRequest
        {
            LoginId = TestLoginId,
            Phone = TestPhone,
            FailOnConflict = false,
            Mfa = true,
        }, TestRefreshJwt);

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.FailOnConflict.Should().BeFalse();
        capturedRequestBody.Mfa.Should().BeTrue();
    }
}
