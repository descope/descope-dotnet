using Descope.Auth.Models.Onetimev1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Unit tests for Enchanted Link authentication using the Kiota-based DescopeClient.
/// These tests use a mock request adapter to simulate API responses without making actual HTTP calls.
/// The SMS routes return PhoneEnchantedLinkResponse, while the email routes return EnchantedLinkResponse.
/// </summary>
public class EnchantedLinkTests
{
    private const string TestRefreshJwt = "test_refresh_jwt";
    private const string TestPhone = "+11234567890";
    private const string TestLoginId = "test-login-id";
    private const string TestRedirectUrl = "https://example.com/verify";

    /// <summary>
    /// Tests that enchanted link SMS sign-up posts the phone sign-up request to the SMS route
    /// and returns the phone-flavoured response.
    /// </summary>
    [Fact]
    public async Task EnchantedLink_SignUpSms_PostsPhoneRequestAndReturnsPhoneResponse()
    {
        // Arrange
        var mockResponse = new PhoneEnchantedLinkResponse
        {
            PendingRef = "pending_ref_signup",
            LinkId = "1",
            MaskedPhone = "+1*******890"
        };

        EnchantedLinkSignUpPhoneRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<EnchantedLinkSignUpPhoneRequest, PhoneEnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/signup/sms");
            capturedRequestBody = requestBody;
            return mockResponse;
        });

        // Act
        var request = new EnchantedLinkSignUpPhoneRequest
        {
            Phone = TestPhone,
            LoginId = TestLoginId,
            RedirectUrl = TestRedirectUrl
        };

        var response = await descopeClient.Auth.V1.Enchantedlink.Signup.Sms.PostAsync(request);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.Phone.Should().Be(TestPhone);
        capturedRequestBody.LoginId.Should().Be(TestLoginId);
        capturedRequestBody.RedirectUrl.Should().Be(TestRedirectUrl);

        response.Should().NotBeNull();
        response!.PendingRef.Should().Be("pending_ref_signup");
        response.LinkId.Should().Be("1");
        response.MaskedPhone.Should().Be("+1*******890");
    }

    /// <summary>
    /// Tests that enchanted link SMS sign-in posts the shared sign-in request to the SMS route
    /// and returns the phone-flavoured response.
    /// </summary>
    [Fact]
    public async Task EnchantedLink_SignInSms_PostsSignInRequestAndReturnsPhoneResponse()
    {
        // Arrange
        var mockResponse = new PhoneEnchantedLinkResponse
        {
            PendingRef = "pending_ref_signin",
            LinkId = "2",
            MaskedPhone = "+1*******890"
        };

        EnchantedLinkSignInRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<EnchantedLinkSignInRequest, PhoneEnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/signin/sms");
            capturedRequestBody = requestBody;
            return mockResponse;
        });

        // Act
        var request = new EnchantedLinkSignInRequest
        {
            LoginId = TestLoginId,
            RedirectUrl = TestRedirectUrl
        };

        var response = await descopeClient.Auth.V1.Enchantedlink.Signin.Sms.PostAsync(request);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.LoginId.Should().Be(TestLoginId);
        capturedRequestBody.RedirectUrl.Should().Be(TestRedirectUrl);

        response.Should().NotBeNull();
        response!.PendingRef.Should().Be("pending_ref_signin");
        response.LinkId.Should().Be("2");
        response.MaskedPhone.Should().Be("+1*******890");
    }

    /// <summary>
    /// Tests that enchanted link SMS sign-up-or-in posts the shared sign-in request to the SMS route
    /// and returns the phone-flavoured response.
    /// </summary>
    [Fact]
    public async Task EnchantedLink_SignUpOrInSms_PostsSignInRequestAndReturnsPhoneResponse()
    {
        // Arrange
        var mockResponse = new PhoneEnchantedLinkResponse
        {
            PendingRef = "pending_ref_signup_in",
            LinkId = "3",
            MaskedPhone = "+1*******890"
        };

        EnchantedLinkSignInRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<EnchantedLinkSignInRequest, PhoneEnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/signup-in/sms");
            capturedRequestBody = requestBody;
            return mockResponse;
        });

        // Act
        var request = new EnchantedLinkSignInRequest
        {
            LoginId = TestLoginId,
            RedirectUrl = TestRedirectUrl
        };

        var response = await descopeClient.Auth.V1.Enchantedlink.SignupIn.Sms.PostAsync(request);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.LoginId.Should().Be(TestLoginId);
        capturedRequestBody.RedirectUrl.Should().Be(TestRedirectUrl);

        response.Should().NotBeNull();
        response!.PendingRef.Should().Be("pending_ref_signup_in");
        response.LinkId.Should().Be("3");
        response.MaskedPhone.Should().Be("+1*******890");
    }

    /// <summary>
    /// Tests that updating a user phone over enchanted link SMS posts to the SMS update route,
    /// carries the refresh JWT, and returns the phone-flavoured response.
    /// </summary>
    [Fact]
    public async Task EnchantedLink_UpdatePhoneSms_SendsRefreshJwtAndReturnsPhoneResponse()
    {
        // Arrange
        var mockResponse = new PhoneEnchantedLinkResponse
        {
            PendingRef = "pending_ref_update",
            LinkId = "4",
            MaskedPhone = "+1*******890"
        };

        UpdateUserPhoneEnchantedLinkRequest? capturedRequestBody = null;
        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UpdateUserPhoneEnchantedLinkRequest, PhoneEnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/update/phone/sms");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            capturedRequestBody = requestBody;
            return mockResponse;
        });

        // Act
        var request = new UpdateUserPhoneEnchantedLinkRequest
        {
            LoginId = TestLoginId,
            Phone = TestPhone,
            RedirectUrl = TestRedirectUrl,
            AddToLoginIDs = true
        };

        var response = await descopeClient.Auth.V1.Enchantedlink.Update.Phone.Sms.PostWithJwtAsync(request, TestRefreshJwt);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody!.LoginId.Should().Be(TestLoginId);
        capturedRequestBody.Phone.Should().Be(TestPhone);
        capturedRequestBody.RedirectUrl.Should().Be(TestRedirectUrl);
        capturedRequestBody.AddToLoginIDs.Should().BeTrue();

        response.Should().NotBeNull();
        response!.PendingRef.Should().Be("pending_ref_update");
        response.LinkId.Should().Be("4");
        response.MaskedPhone.Should().Be("+1*******890");
    }

    /// <summary>
    /// Tests that the phone update rejects a missing refresh JWT before issuing a request.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task EnchantedLink_UpdatePhoneSms_ThrowsWhenRefreshJwtMissing(string? refreshJwt)
    {
        // Arrange
        var descopeClient = TestDescopeClientFactory.CreateWithResponse(new PhoneEnchantedLinkResponse());

        // Act
        Func<Task> act = () => descopeClient.Auth.V1.Enchantedlink.Update.Phone.Sms.PostWithJwtAsync(
            new UpdateUserPhoneEnchantedLinkRequest { LoginId = TestLoginId, Phone = TestPhone },
            refreshJwt!);

        // Assert
        await act.Should().ThrowAsync<DescopeException>();
    }

    /// <summary>
    /// Tests that the pre-existing email update route still posts to the email path and returns the
    /// email-flavoured response, so the SMS addition does not change the email channel's contract.
    /// </summary>
    [Fact]
    public async Task EnchantedLink_UpdateEmail_SendsRefreshJwtAndReturnsEmailResponse()
    {
        // Arrange
        var mockResponse = new EnchantedLinkResponse
        {
            PendingRef = "pending_ref_email",
            LinkId = "5",
            MaskedEmail = "t***@example.com"
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<UpdateUserEmailEnchantedLinkRequest, EnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/update/email");
            AssertJwtOption(requestInfo, TestRefreshJwt);
            return mockResponse;
        });

        // Act
        var request = new UpdateUserEmailEnchantedLinkRequest
        {
            LoginId = TestLoginId,
            Email = "test@example.com",
            RedirectUrl = TestRedirectUrl
        };

        var response = await descopeClient.Auth.V1.Enchantedlink.Update.Email.PostWithJwtAsync(request, TestRefreshJwt);

        // Assert
        response.Should().NotBeNull();
        response!.PendingRef.Should().Be("pending_ref_email");
        response.LinkId.Should().Be("5");
        response.MaskedEmail.Should().Be("t***@example.com");
    }

    private static void AssertJwtOption(RequestInformation requestInfo, string expectedJwt)
    {
        var jwtOption = requestInfo.RequestOptions.OfType<DescopeJwtOption>().SingleOrDefault();
        jwtOption.Should().NotBeNull();
        jwtOption!.GetContext()["jwt"].Should().Be(expectedJwt);
    }
}
