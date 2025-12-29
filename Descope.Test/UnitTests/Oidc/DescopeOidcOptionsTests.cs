#if NET6_0_OR_GREATER
using Xunit;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Text.Json;

namespace Descope.Test.UnitTests.Oidc
{
    public class DescopeOidcOptionsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var options = new DescopeOidcOptions();

            Assert.Equal(string.Empty, options.ProjectId);
            Assert.Null(options.ClientSecret);
            Assert.Equal("https://api.descope.com", options.BaseUrl);
            Assert.Equal("/signin-descope", options.CallbackPath);
            Assert.Equal("/signout-callback-descope", options.SignedOutCallbackPath);
            Assert.Null(options.PostLogoutRedirectUri);
            Assert.Equal("openid profile email", options.Scope);
            Assert.True(options.UsePkce);
            Assert.True(options.SaveTokens);
            Assert.False(options.GetClaimsFromUserInfoEndpoint);
            Assert.Equal("Descope", options.AuthenticationScheme);
        }

        [Fact]
        public void BaseUrl_FallsBackToDefault_WhenSetToEmptyString()
        {
            var options = new DescopeOidcOptions
            {
                ProjectId = "P123456789",
                BaseUrl = ""
            };

            Assert.Equal("https://api.descope.com", options.BaseUrl);
        }

        [Fact]
        public void BaseUrl_FallsBackToDefault_WhenSetToNull()
        {
            var options = new DescopeOidcOptions
            {
                ProjectId = "P123456789",
                BaseUrl = null
            };

            Assert.Equal("https://api.descope.com", options.BaseUrl);
        }

        [Fact]
        public void BaseUrl_FallsBackToRegionalUrl_WhenSetToEmptyWithRegionalProjectId()
        {
            // 32+ character project ID with region "use1" at positions 1-4
            var options = new DescopeOidcOptions
            {
                ProjectId = "Puse1567890123456789012345678901",
                BaseUrl = ""
            };

            Assert.Equal("https://api.use1.descope.com", options.BaseUrl);
        }

        [Fact]
        public void Validate_ThrowsWhenProjectIdIsEmpty()
        {
            var options = new DescopeOidcOptions();

            var ex = Assert.Throws<DescopeException>(() => options.Validate());
            Assert.Contains("ProjectId is required", ex.Message);
        }

        [Fact]
        public void Validate_ThrowsWhenProjectIdIsWhitespace()
        {
            var options = new DescopeOidcOptions { ProjectId = "   " };

            var ex = Assert.Throws<DescopeException>(() => options.Validate());
            Assert.Contains("ProjectId is required", ex.Message);
        }

        [Fact]
        public void Validate_PassesWithValidProjectId()
        {
            var options = new DescopeOidcOptions { ProjectId = "P123456789" };

            options.Validate(); // Should not throw
        }

        [Fact]
        public void GetAuthority_UsesDefaultBaseUrl_WhenBaseUrlIsNull()
        {
            var options = new DescopeOidcOptions
            {
                ProjectId = "P123456789",
                BaseUrl = null
            };

            var authority = options.GetAuthority();

            Assert.Equal("https://api.descope.com/P123456789", authority);
        }

        [Fact]
        public void GetAuthority_UsesProvidedBaseUrl()
        {
            var options = new DescopeOidcOptions
            {
                ProjectId = "P123456789",
                BaseUrl = "https://custom.example.com"
            };

            var authority = options.GetAuthority();

            Assert.Equal("https://custom.example.com/P123456789", authority);
        }

        [Fact]
        public void GetAuthority_TrimsTrailingSlash()
        {
            var options = new DescopeOidcOptions
            {
                ProjectId = "P123456789",
                BaseUrl = "https://custom.example.com/"
            };

            var authority = options.GetAuthority();

            Assert.Equal("https://custom.example.com/P123456789", authority);
        }

        [Fact]
        public void GetAuthority_UsesRegionalUrl_ForLongProjectId()
        {
            // 32+ character project ID with region at positions 1-4
            var options = new DescopeOidcOptions
            {
                ProjectId = "Peus1abcdefghijklmnopqrstuvwxyz12",
                BaseUrl = null
            };

            var authority = options.GetAuthority();

            Assert.Equal("https://api.eus1.descope.com/Peus1abcdefghijklmnopqrstuvwxyz12", authority);
        }
    }
}
#endif
