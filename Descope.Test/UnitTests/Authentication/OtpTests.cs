using System.Text;
using Descope.Auth.Models.Onetimev1;
using FluentAssertions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using Xunit;

namespace Descope.Test.UnitTests.Authentication;

/// <summary>
/// Unit tests for OTP request and response fields.
/// </summary>
public class OtpTests
{
    private static string Serialize(IParsable model)
    {
        using var writer = new JsonSerializationWriter();
        writer.WriteObjectValue(null, model);
        using var reader = new StreamReader(writer.GetSerializedContent(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Fact]
    public void OTPSignInRequest_SerializesUseRecovery()
    {
        var json = Serialize(new OTPSignInRequest { LoginId = "user@example.com", UseRecovery = true });

        json.Should().Contain("\"useRecovery\":true");
    }

    [Fact]
    public void OTPSignInRequest_OmitsUseRecoveryWhenUnset()
    {
        var json = Serialize(new OTPSignInRequest { LoginId = "user@example.com" });

        json.Should().NotContain("useRecovery");
    }

    [Fact]
    public void UpdateUserEmailOTPRequest_SerializesFailOnConflictAndMfa()
    {
        var json = Serialize(new UpdateUserEmailOTPRequest { LoginId = "user@example.com", Email = "new@example.com", FailOnConflict = true, Mfa = true });

        json.Should().Contain("\"failOnConflict\":true").And.Contain("\"mfa\":true");
    }

    [Fact]
    public void UpdateUserPhoneOTPRequest_SerializesFailOnConflictAndMfa()
    {
        var json = Serialize(new UpdateUserPhoneOTPRequest { LoginId = "user@example.com", Phone = "+15555555555", FailOnConflict = true, Mfa = true });

        json.Should().Contain("\"failOnConflict\":true").And.Contain("\"mfa\":true");
    }

    [Fact]
    public void JWTResponse_ParsesNewFields()
    {
        const string json = "{\"sessionJwt\":\"s\",\"trustedDeviceJwt\":\"t\",\"nextRefreshSeconds\":60,\"cookieName\":\"DSR\",\"sessionCookieName\":\"DS\",\"sessionCookieDomain\":\"example.com\",\"unsavedSSO\":true,\"knownDeviceJwt\":\"k\",\"ssoAuthenticationOnly\":true,\"userCreationDeferred\":true,\"unknown\":1}";
        var node = new JsonParseNodeFactory().GetRootParseNodeAsync("application/json", new MemoryStream(Encoding.UTF8.GetBytes(json))).GetAwaiter().GetResult();

        var response = node.GetObjectValue(JWTResponse.CreateFromDiscriminatorValue);

        response.Should().NotBeNull();
        response!.SessionJwt.Should().Be("s");
        response.TrustedDeviceJwt.Should().Be("t");
        response.NextRefreshSeconds.Should().Be(60);
        response.CookieName.Should().Be("DSR");
        response.SessionCookieName.Should().Be("DS");
        response.SessionCookieDomain.Should().Be("example.com");
        response.UnsavedSSO.Should().BeTrue();
        response.KnownDeviceJwt.Should().Be("k");
        response.SsoAuthenticationOnly.Should().BeTrue();
        response.UserCreationDeferred.Should().BeTrue();
    }
}
