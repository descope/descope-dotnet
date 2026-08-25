using Descope.Auth;
using Descope.Auth.Models.Onetimev1;
using Descope.Test.Helpers;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Unit tests for Enchanted Link authentication using the Kiota-based DescopeClient.
/// Covers both the email delivery method and the additive SMS delivery method,
/// which was split server-side into EmailEnchantedLinkResponse/PhoneEnchantedLinkResponse.
/// These tests use a mock request adapter to simulate API responses without making actual HTTP calls.
/// </summary>
public class EnchantedLinkTests
{
    [Fact]
    public async Task EnchantedLink_SignIn_Email_Success()
    {
        var mockResponse = new EmailEnchantedLinkResponse
        {
            LinkId = "link123",
            PendingRef = "pending123",
            MaskedEmail = "t***@example.com"
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<EnchantedLinkSignInRequest, EmailEnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/signin/email");
            requestBody!.LoginId.Should().Be("test@example.com");
            return mockResponse;
        });

        var request = new EnchantedLinkSignInRequest { LoginId = "test@example.com" };
        var response = await descopeClient.Auth.V1.Enchantedlink.Signin.Email.PostAsync(request);

        response.Should().NotBeNull();
        response!.LinkId.Should().Be("link123");
        response.PendingRef.Should().Be("pending123");
        response.MaskedEmail.Should().Be("t***@example.com");
    }

    [Fact]
    public async Task EnchantedLink_SignIn_Sms_Success()
    {
        var mockResponse = new PhoneEnchantedLinkResponse
        {
            LinkId = "link456",
            PendingRef = "pending456",
            MaskedPhone = "+1******89"
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<EnchantedLinkSignInRequest, PhoneEnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/signin/sms");
            requestBody!.LoginId.Should().Be("+12345678989");
            return mockResponse;
        });

        var request = new EnchantedLinkSignInRequest { LoginId = "+12345678989" };
        var response = await descopeClient.Auth.V1.Enchantedlink.Signin.Sms.PostAsync(request);

        response.Should().NotBeNull();
        response!.LinkId.Should().Be("link456");
        response.PendingRef.Should().Be("pending456");
        response.MaskedPhone.Should().Be("+1******89");
    }

    [Fact]
    public async Task EnchantedLink_SignUp_Sms_Success()
    {
        var mockResponse = new PhoneEnchantedLinkResponse
        {
            LinkId = "link789",
            PendingRef = "pending789",
            MaskedPhone = "+1******89"
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<EnchantedLinkSignUpPhoneRequest, PhoneEnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/signup/sms");
            requestBody!.Phone.Should().Be("+12345678989");
            return mockResponse;
        });

        var request = new EnchantedLinkSignUpPhoneRequest { Phone = "+12345678989" };
        var response = await descopeClient.Auth.V1.Enchantedlink.Signup.Sms.PostAsync(request);

        response.Should().NotBeNull();
        response!.LinkId.Should().Be("link789");
        response.MaskedPhone.Should().Be("+1******89");
    }

    [Fact]
    public async Task EnchantedLink_SignUpOrIn_Sms_Success()
    {
        var mockResponse = new PhoneEnchantedLinkResponse
        {
            LinkId = "linkabc",
            PendingRef = "pendingabc",
            MaskedPhone = "+1******89"
        };

        var descopeClient = TestDescopeClientFactory.CreateWithAsserter<EnchantedLinkSignInRequest, PhoneEnchantedLinkResponse>((requestInfo, requestBody) =>
        {
            requestInfo.HttpMethod.Should().Be(Method.POST);
            requestInfo.URI.AbsolutePath.Should().EndWith("/v1/auth/enchantedlink/signup-in/sms");
            requestBody!.LoginId.Should().Be("+12345678989");
            return mockResponse;
        });

        var request = new EnchantedLinkSignInRequest { LoginId = "+12345678989" };
        var response = await descopeClient.Auth.V1.Enchantedlink.SignupIn.Sms.PostAsync(request);

        response.Should().NotBeNull();
        response!.LinkId.Should().Be("linkabc");
        response.MaskedPhone.Should().Be("+1******89");
    }
}
