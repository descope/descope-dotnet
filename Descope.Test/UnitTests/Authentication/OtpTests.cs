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
    [Fact]
    public async Task Otp_SignUpEmail_SendsTenantId()
    {
        // Arrange
        OTPSignUpEmailRequest? captured = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignUpEmailRequest, EmailOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signup/email");
            captured = requestBody;
            return new EmailOperationResponse { MaskedEmail = "t***@example.com" };
        });

        // Act
        var response = await descopeClient.Auth.V1.Otp.Signup.Email.PostAsync(new OTPSignUpEmailRequest
        {
            LoginId = "test@example.com",
            Email = "test@example.com",
            LoginOptions = new SignupLoginOptions { TenantId = "tenant123" },
        });

        // Assert
        response!.MaskedEmail.Should().Be("t***@example.com");
        captured.Should().NotBeNull();
        captured!.LoginId.Should().Be("test@example.com");
        captured.LoginOptions!.TenantId.Should().Be("tenant123");
    }

    [Fact]
    public async Task Otp_SignUpSms_SendsTenantId()
    {
        // Arrange
        OTPSignUpPhoneRequest? captured = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignUpPhoneRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signup/sms");
            captured = requestBody;
            return new PhoneOperationResponse { MaskedPhone = "+1******89" };
        });

        // Act
        var response = await descopeClient.Auth.V1.Otp.Signup.Sms.PostAsync(new OTPSignUpPhoneRequest
        {
            LoginId = "+15555555589",
            Phone = "+15555555589",
            LoginOptions = new SignupLoginOptions { TenantId = "tenant123" },
        });

        // Assert
        response!.MaskedPhone.Should().Be("+1******89");
        captured!.LoginOptions!.TenantId.Should().Be("tenant123");
    }

    [Fact]
    public async Task Otp_SignIn_SendsTenantIdAndUseRecovery()
    {
        // Arrange
        OTPSignInRequest? captured = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignInRequest, EmailOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signin/email");
            captured = requestBody;
            return new EmailOperationResponse { MaskedEmail = "t***@example.com" };
        });

        // Act
        await descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(new OTPSignInRequest
        {
            LoginId = "test@example.com",
            UseRecovery = true,
            LoginOptions = new LoginOptions { TenantId = "tenant123" },
        });

        // Assert
        captured!.LoginId.Should().Be("test@example.com");
        captured.UseRecovery.Should().BeTrue();
        captured.LoginOptions!.TenantId.Should().Be("tenant123");
    }

    [Fact]
    public async Task Otp_SignUpOrIn_SendsTenantIdAndUseRecovery()
    {
        // Arrange
        OTPSignInRequest? captured = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignInRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signup-in/voice");
            captured = requestBody;
            return new PhoneOperationResponse { MaskedPhone = "+1******89" };
        });

        // Act
        await descopeClient.Auth.V1.Otp.SignupIn.Voice.PostAsync(new OTPSignInRequest
        {
            LoginId = "+15555555589",
            UseRecovery = false,
            LoginOptions = new LoginOptions { TenantId = "tenant123" },
        });

        // Assert
        captured!.UseRecovery.Should().BeFalse();
        captured.LoginOptions!.TenantId.Should().Be("tenant123");
    }

    [Fact]
    public async Task Otp_SignIn_OmitsUnsetFields()
    {
        // Arrange
        OTPSignInRequest? captured = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignInRequest, EmailOperationResponse>((_, requestBody) =>
        {
            captured = requestBody;
            return new EmailOperationResponse();
        });

        // Act
        await descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(new OTPSignInRequest { LoginId = "test@example.com" });

        // Assert
        captured!.UseRecovery.Should().BeNull();
        captured.LoginOptions.Should().BeNull();
    }

    [Fact]
    public async Task Otp_VerifyEmail_DeserializesNewFields()
    {
        // Arrange
        var mockResponse = new JWTResponse
        {
            SessionJwt = "session_jwt",
            RefreshJwt = "refresh_jwt",
            TrustedDeviceJwt = "trusted_device_jwt",
            NextRefreshSeconds = 60,
            CookieName = "DSR",
            SessionCookieName = "DS",
            SessionCookieDomain = "example.com",
            UnsavedSSO = true,
            KnownDeviceJwt = "known_device_jwt",
            SsoAuthenticationOnly = true,
            UserCreationDeferred = true,
            IdpResponse = new IDPResponse
            {
                IdpOIDCClaims = new IDPResponse_idpOIDCClaims
                {
                    AdditionalData = new Dictionary<string, object> { { "sub", "oidc-sub" } },
                },
            },
            User = new ResponseUser
            {
                UserId = "user123",
                UserTenants = new List<UserTenants>
                {
                    new UserTenants { TenantId = "tenant123", RoleIds = new List<string> { "role1" } },
                },
                ConsentExpiration = 100,
                RecoveryEmail = "recovery@example.com",
                VerifiedRecoveryEmail = true,
                RecoveryPhone = "+15555555589",
                VerifiedRecoveryPhone = true,
                ModifiedTime = 200,
                RoleIds = new List<string> { "role2" },
                RecoveryCodes = true,
                Dependent = true,
                UserFamilies = new List<UserFamilies>
                {
                    new UserFamilies
                    {
                        FamilyId = "family1",
                        RoleNames = new List<string> { "parent" },
                        Permissions = new List<string> { "perm1" },
                        FamilyScopedAttributes = new UserFamilies_familyScopedAttributes
                        {
                            AdditionalData = new Dictionary<string, object> { { "attr", "value" } },
                        },
                    },
                },
                LockReason = "too_many_attempts",
                TempLockExpiration = 300,
                ScimTenantId = "scim_tenant",
                SamlTenantId = "saml_tenant",
                OidcTenantId = "oidc_tenant",
                ConsentTenantId = "consent_tenant",
                LastLoginTime = 400,
            },
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPVerifyCodeRequest, JWTResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/verify/email");
            requestBody!.LoginId.Should().Be("test@example.com");
            requestBody.Code.Should().Be("123456");
            return mockResponse;
        });

        // Act
        var response = await descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(new OTPVerifyCodeRequest
        {
            LoginId = "test@example.com",
            Code = "123456",
        });

        // Assert
        response.Should().NotBeNull();
        response!.TrustedDeviceJwt.Should().Be("trusted_device_jwt");
        response.NextRefreshSeconds.Should().Be(60);
        response.CookieName.Should().Be("DSR");
        response.SessionCookieName.Should().Be("DS");
        response.SessionCookieDomain.Should().Be("example.com");
        response.UnsavedSSO.Should().BeTrue();
        response.KnownDeviceJwt.Should().Be("known_device_jwt");
        response.SsoAuthenticationOnly.Should().BeTrue();
        response.UserCreationDeferred.Should().BeTrue();
        response.IdpResponse!.IdpOIDCClaims!.AdditionalData.Should().ContainKey("sub");

        var user = response.User!;
        user.UserTenants![0].RoleIds.Should().Equal("role1");
        user.ConsentExpiration.Should().Be(100);
        user.RecoveryEmail.Should().Be("recovery@example.com");
        user.VerifiedRecoveryEmail.Should().BeTrue();
        user.RecoveryPhone.Should().Be("+15555555589");
        user.VerifiedRecoveryPhone.Should().BeTrue();
        user.ModifiedTime.Should().Be(200);
        user.RoleIds.Should().Equal("role2");
        user.RecoveryCodes.Should().BeTrue();
        user.Dependent.Should().BeTrue();
        user.UserFamilies.Should().HaveCount(1);
        user.UserFamilies![0].FamilyId.Should().Be("family1");
        user.UserFamilies[0].RoleNames.Should().Equal("parent");
        user.UserFamilies[0].Permissions.Should().Equal("perm1");
        user.UserFamilies[0].FamilyScopedAttributes!.AdditionalData.Should().ContainKey("attr");
        user.LockReason.Should().Be("too_many_attempts");
        user.TempLockExpiration.Should().Be(300);
        user.ScimTenantId.Should().Be("scim_tenant");
        user.SamlTenantId.Should().Be("saml_tenant");
        user.OidcTenantId.Should().Be("oidc_tenant");
        user.ConsentTenantId.Should().Be("consent_tenant");
        user.LastLoginTime.Should().Be(400);
    }

    [Fact]
    public async Task Otp_VerifySms_AcceptsResponseWithoutNewFields()
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPVerifyCodeRequest, JWTResponse>((requestInfo, _) =>
        {
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/verify/sms");
            return new JWTResponse { SessionJwt = "session_jwt", User = new ResponseUser { UserId = "user123" } };
        });

        // Act
        var response = await descopeClient.Auth.V1.Otp.Verify.Sms.PostAsync(new OTPVerifyCodeRequest
        {
            LoginId = "+15555555589",
            Code = "123456",
        });

        // Assert
        response!.SessionJwt.Should().Be("session_jwt");
        response.TrustedDeviceJwt.Should().BeNull();
        response.User!.UserFamilies.Should().BeNull();
        response.User.LastLoginTime.Should().BeNull();
    }

    [Fact]
    public async Task Otp_UpdateEmail_SendsFailOnConflictAndMfa()
    {
        // Arrange
        UpdateUserEmailOTPRequest? captured = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UpdateUserEmailOTPRequest, EmailOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/update/email");
            requestInfo.RequestOptions.OfType<DescopeJwtOption>().Should().ContainSingle();
            captured = requestBody;
            return new EmailOperationResponse { MaskedEmail = "n***@example.com" };
        });

        // Act
        var response = await descopeClient.Auth.V1.Otp.Update.Email.PostWithJwtAsync(new UpdateUserEmailOTPRequest
        {
            LoginId = "test@example.com",
            Email = "new@example.com",
            FailOnConflict = true,
            Mfa = true,
        }, "refresh_jwt");

        // Assert
        response!.MaskedEmail.Should().Be("n***@example.com");
        captured!.Email.Should().Be("new@example.com");
        captured.FailOnConflict.Should().BeTrue();
        captured.Mfa.Should().BeTrue();
    }

    [Fact]
    public async Task Otp_UpdatePhoneSms_SendsFailOnConflictAndMfa()
    {
        // Arrange
        UpdateUserPhoneOTPRequest? captured = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UpdateUserPhoneOTPRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/update/phone/sms");
            requestInfo.RequestOptions.OfType<DescopeJwtOption>().Should().ContainSingle();
            captured = requestBody;
            return new PhoneOperationResponse { MaskedPhone = "+1******89" };
        });

        // Act
        await descopeClient.Auth.V1.Otp.Update.Phone.Sms.PostWithJwtAsync(new UpdateUserPhoneOTPRequest
        {
            LoginId = "test@example.com",
            Phone = "+15555555589",
            FailOnConflict = false,
            Mfa = true,
        }, "refresh_jwt");

        // Assert
        captured!.Phone.Should().Be("+15555555589");
        captured.FailOnConflict.Should().BeFalse();
        captured.Mfa.Should().BeTrue();
    }
}
