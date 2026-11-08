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

    [Fact]
    public async Task Otp_SignIn_SendsUseRecoveryAndTenantId()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignInRequest, EmailOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signin/email");
            requestBody.Should().NotBeNull();
            requestBody!.LoginId.Should().Be("test@example.com");
            requestBody.UseRecovery.Should().BeTrue();
            requestBody.LoginOptions!.TenantId.Should().Be("tenant1");
            return new EmailOperationResponse { MaskedEmail = "t***@example.com" };
        });

        var request = new OTPSignInRequest
        {
            LoginId = "test@example.com",
            UseRecovery = true,
            LoginOptions = new LoginOptions { TenantId = "tenant1" }
        };
        var response = await descopeClient.Auth.V1.Otp.Signin.Email.PostAsync(request);

        response.Should().NotBeNull();
        response!.MaskedEmail.Should().Be("t***@example.com");
    }

    [Fact]
    public async Task Otp_SignUpIn_SendsUseRecoveryAndTenantId()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignInRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signup-in/sms");
            requestBody!.UseRecovery.Should().BeTrue();
            requestBody.LoginOptions!.TenantId.Should().Be("tenant1");
            return new PhoneOperationResponse { MaskedPhone = "+1*****1234" };
        });

        var request = new OTPSignInRequest
        {
            LoginId = "+15555551234",
            UseRecovery = true,
            LoginOptions = new LoginOptions { TenantId = "tenant1" }
        };
        var response = await descopeClient.Auth.V1.Otp.SignupIn.Sms.PostAsync(request);

        response!.MaskedPhone.Should().Be("+1*****1234");
    }

    [Fact]
    public async Task Otp_SignUp_SendsLoginOptionsTenantId()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPSignUpPhoneRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/signup/sms");
            requestBody!.Phone.Should().Be("+15555551234");
            requestBody.LoginOptions!.TenantId.Should().Be("tenant1");
            return new PhoneOperationResponse { MaskedPhone = "+1*****1234" };
        });

        var request = new OTPSignUpPhoneRequest
        {
            LoginId = "+15555551234",
            Phone = "+15555551234",
            LoginOptions = new SignupLoginOptions { TenantId = "tenant1" }
        };
        var response = await descopeClient.Auth.V1.Otp.Signup.Sms.PostAsync(request);

        response!.MaskedPhone.Should().Be("+1*****1234");
    }

    [Fact]
    public async Task Otp_Verify_DeserializesNewFields()
    {
        var mockResponse = new JWTResponse
        {
            SessionJwt = "session_jwt",
            TrustedDeviceJwt = "trusted_device_jwt",
            NextRefreshSeconds = 300,
            CookieName = "DSR",
            SessionCookieName = "DS",
            SessionCookieDomain = "example.com",
            UnsavedSSO = true,
            KnownDeviceJwt = "known_device_jwt",
            SsoAuthenticationOnly = true,
            UserCreationDeferred = true,
            IdpResponse = new IDPResponse
            {
                IdpOIDCClaims = new IDPResponse_idpOIDCClaims { AdditionalData = new Dictionary<string, object> { { "sub", "abc" } } }
            },
            User = new ResponseUser
            {
                UserId = "user123",
                UserTenants = new List<UserTenants> { new UserTenants { TenantId = "tenant1", RoleIds = new List<string> { "role1" } } },
                ConsentExpiration = 1,
                RecoveryEmail = "recovery@example.com",
                VerifiedRecoveryEmail = true,
                RecoveryPhone = "+15555551234",
                VerifiedRecoveryPhone = true,
                ModifiedTime = 2,
                RoleIds = new List<string> { "role2" },
                RecoveryCodes = true,
                Dependent = true,
                UserFamilies = new List<UserFamilies> { new UserFamilies { FamilyId = "family1", RoleNames = new List<string> { "parent" } } },
                LockReason = "manual",
                TempLockExpiration = 3,
                ScimTenantId = "scim_tenant",
                SamlTenantId = "saml_tenant",
                OidcTenantId = "oidc_tenant",
                ConsentTenantId = "consent_tenant",
                LastLoginTime = 4
            }
        };
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<OTPVerifyCodeRequest, JWTResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/verify/email");
            requestBody!.Code.Should().Be("123456");
            return mockResponse;
        });

        var response = await descopeClient.Auth.V1.Otp.Verify.Email.PostAsync(new OTPVerifyCodeRequest { LoginId = "test@example.com", Code = "123456" });

        response.Should().NotBeNull();
        response!.TrustedDeviceJwt.Should().Be("trusted_device_jwt");
        response.NextRefreshSeconds.Should().Be(300);
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
        user.ConsentExpiration.Should().Be(1);
        user.RecoveryEmail.Should().Be("recovery@example.com");
        user.VerifiedRecoveryEmail.Should().BeTrue();
        user.RecoveryPhone.Should().Be("+15555551234");
        user.VerifiedRecoveryPhone.Should().BeTrue();
        user.ModifiedTime.Should().Be(2);
        user.RoleIds.Should().Equal("role2");
        user.RecoveryCodes.Should().BeTrue();
        user.Dependent.Should().BeTrue();
        user.UserFamilies![0].FamilyId.Should().Be("family1");
        user.UserFamilies[0].RoleNames.Should().Equal("parent");
        user.LockReason.Should().Be("manual");
        user.TempLockExpiration.Should().Be(3);
        user.ScimTenantId.Should().Be("scim_tenant");
        user.SamlTenantId.Should().Be("saml_tenant");
        user.OidcTenantId.Should().Be("oidc_tenant");
        user.ConsentTenantId.Should().Be("consent_tenant");
        user.LastLoginTime.Should().Be(4);
    }

    [Fact]
    public async Task Otp_UpdateEmail_PostWithJwt_SendsFailOnConflictAndMfa()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UpdateUserEmailOTPRequest, EmailOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/update/email");
            var jwtOption = requestInfo.RequestOptions.OfType<DescopeJwtOption>().SingleOrDefault();
            jwtOption.Should().NotBeNull();
            jwtOption!.GetContext()["jwt"].Should().Be(TestRefreshJwt);
            requestBody!.FailOnConflict.Should().BeTrue();
            requestBody.Mfa.Should().BeTrue();
            return new EmailOperationResponse { MaskedEmail = "n***@example.com" };
        });

        var request = new UpdateUserEmailOTPRequest { LoginId = "test@example.com", Email = "new@example.com", FailOnConflict = true, Mfa = true };
        var response = await descopeClient.Auth.V1.Otp.Update.Email.PostWithJwtAsync(request, TestRefreshJwt);

        response!.MaskedEmail.Should().Be("n***@example.com");
    }

    [Fact]
    public async Task Otp_UpdatePhoneIm_PostWithJwt_SendsRefreshJwt()
    {
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UpdateUserPhoneOTPRequest, PhoneOperationResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/otp/update/phone/im");
            var jwtOption = requestInfo.RequestOptions.OfType<DescopeJwtOption>().SingleOrDefault();
            jwtOption.Should().NotBeNull();
            jwtOption!.GetContext()["jwt"].Should().Be(TestRefreshJwt);
            requestBody!.Phone.Should().Be("+15555551234");
            requestBody.FailOnConflict.Should().BeTrue();
            requestBody.Mfa.Should().BeTrue();
            return new PhoneOperationResponse { MaskedPhone = "+1*****1234" };
        });

        var request = new UpdateUserPhoneOTPRequest { LoginId = "test@example.com", Phone = "+15555551234", FailOnConflict = true, Mfa = true };
        var response = await descopeClient.Auth.V1.Otp.Update.Phone.Im.PostWithJwtAsync(request, TestRefreshJwt);

        response!.MaskedPhone.Should().Be("+1*****1234");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Otp_UpdatePhoneIm_PostWithJwt_ThrowsWhenRefreshJwtMissing(string? refreshJwt)
    {
        var descopeClient = TestDescopeClientFactory.CreateWithEmptyResponse();

        Func<Task> act = () => descopeClient.Auth.V1.Otp.Update.Phone.Im.PostWithJwtAsync(new UpdateUserPhoneOTPRequest(), refreshJwt!);

        await act.Should().ThrowAsync<DescopeException>();
    }
}
