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

    public class TokenEndpointResponseTests
    {
        /// <summary>
        /// Helper method that simulates how ASP.NET Core OIDC middleware parses
        /// the token endpoint JSON response into an OpenIdConnectMessage.
        /// This mirrors the behavior in Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectHandler.
        /// </summary>
        private static Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage ParseTokenResponseJson(string json)
        {
            var message = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage();

            using var document = JsonDocument.Parse(json);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                // The OIDC middleware uses SetParameter to populate the message from JSON
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    message.SetParameter(property.Name, property.Value.GetString());
                }
                else if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    message.SetParameter(property.Name, property.Value.GetRawText());
                }
                else if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
                {
                    message.SetParameter(property.Name, property.Value.GetBoolean().ToString().ToLowerInvariant());
                }
            }

            return message;
        }

        /// <summary>
        /// Verifies that when a token response is received from the /oauth2/v1/token endpoint,
        /// the IdToken is correctly parsed and is not null.
        /// Covers a bug in earlier versions of Microsoft.IdentityModel.Protocols.OpenIdConnect in which the IdToken property was not set correctly.
        /// </summary>
        [Fact]
        public void TokenEndpointResponse_IdToken_IsNotNull()
        {
            // Arrange - Simulate the raw JSON response from /oauth2/v1/token
            var tokenResponseJson = @"{
                ""access_token"": ""eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMzZaMlg0TE93dFlVRHp5b1MwTk9ndWxQMHdoIiwidHlwIjoiSldUIn0.eyJhbXIiOlsiZW1haWwiXSwiYXVkIjpbIlAzNloyV3hUdmVNUUJXbExmQ01QbEMzUU0xT2EiXSwiYXpwIjoiUDM2WjJXeFR2ZU1RQldsTGZDTVBsQzNRTTFPYSIsImVtYWlsIjoieW9zaSsxQGRlc2NvcGUuY29tIiwiZXhwIjoxNzY2OTEyNzk4LCJpYXQiOjE3NjY5MTIxOTgsImlzcyI6Imh0dHBzOi8veW9zaS5kZXNjb3BlLnRlYW0vUDM2WjJXeFR2ZU1RQldsTGZDTVBsQzNRTTFPYSIsInJleHAiOiIyMDI2LTAxLTI1VDA4OjU2OjM4WiIsInNjb3BlIjoib3BlbmlkIHByb2ZpbGUgZW1haWwiLCJzdWIiOiJVMzdMM1VqdUFsSmdISmtZR1BoM1o0bkxjZG5nIiwidG9rZW5fdHlwZSI6ImFjY2Vzc190b2tlbiJ9.TGKo6HXdb-j_E6vhfLLQ--ELpCTQcYtstEpz6rq77zCgtFyzhUxX4xmRdQuvvn2PXdAq4XoWY1wpRek448Me40OKKFHvgbkx12OrGphRTvPfcP8jz-WCBskehumT_O43s8nQeEFYhW2pvzPeflfdzMP_YVGgFOVIDAQ_fhwuJ18gRXh3X0I75mGGVFeWv_7lqwqAQB-UIaLRTzI47M1CKNWUg37o7tOcxmFjrIRXh9hIK6hWhQN_PWTynkMRzWTzC0XyhHrQWjzybnmxVKQLO0zkGrOa2qstu8_0oguiW6xGkEFiNAGwHqtiFbQhlwNs2AShLK9wOpniEsSmEAtonA"",
                ""token_type"": ""Bearer"",
                ""refresh_token"": ""eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMzZaMlg0TE93dFlVRHp5b1MwTk9ndWxQMHdoIiwidHlwIjoiSldUIn0.eyJhbXIiOlsiZW1haWwiXSwiYXVkIjpbIlAzNloyV3hUdmVNUUJXbExmQ01QbEMzUU0xT2EiXSwiYXpwIjoiUDM2WjJXeFR2ZU1RQldsTGZDTVBsQzNRTTFPYSIsImRjbCI6WyJlbWFpbCJdLCJkdiI6MSwiZW1haWwiOiJ5b3NpKzFAZGVzY29wZS5jb20iLCJleHAiOjE3NjkzMzEzOTgsImlhdCI6MTc2NjkxMjE5OCwiaXNzIjoiaHR0cHM6Ly95b3NpLmRlc2NvcGUudGVhbS9QMzZaMld4VHZlTVFCV2xMZkNNUGxDM1FNMU9hIiwic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSBlbWFpbCIsInN1YiI6IlUzN0wzVWp1QWxKZ0hKa1lHUGgzWjRuTGNkbmciLCJ0b2tlbl90eXBlIjoicmVmcmVzaF90b2tlbiJ9.BB3fJR213LAiUqyQzJoAyLePxviVjPcW_O0cboCS_Mjs68BkKOVi9scybrnAWZMcbQbkIwtNaac3DC3fUKi1iA4uu_fuAwxaUvbiruMWRBwi4Ozuf5ssAVPDw303RIvAc0wgp11TgglTGjJW4zhNIbYUT-0GATcviB_szpEY67tDwAXTvSFVDyP1RBx7IzCR_DrgUHr3PcvjLxBsyQOxXqa_8x6s-fMIIA_AQgjzIMFHPBp3IcYiEwqby8p44B9692BJ1cBEvBnZJlPrviv7FOk7BEfm3JgYEyf-LhuanJO0yxYnQ6djQi1GrLCxVi793uEmEMImRiWyOrt093dkqA"",
                ""id_token"": ""eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMzZaMlg0TE93dFlVRHp5b1MwTk9ndWxQMHdoIiwidHlwIjoiSldUIn0.eyJhdWQiOlsiUDM2WjJXeFR2ZU1RQldsTGZDTVBsQzNRTTFPYSJdLCJhenAiOiJQMzZaMld4VHZlTVFCV2xMZkNNUGxDM1FNMU9hIiwiZW1haWwiOiJ5b3NpKzFAZGVzY29wZS5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiZXhwIjoxNzY2OTEyNzk4LCJpYXQiOjE3NjY5MTIxOTgsImlzcyI6Imh0dHBzOi8veW9zaS5kZXNjb3BlLnRlYW0vUDM2WjJXeFR2ZU1RQldsTGZDTVBsQzNRTTFPYSIsIm5vbmNlIjoiNjM5MDI1MDg5ODQ5OTQwMTMwLlpqWXlZemRrTVRBdFltSTNNaTAwT0dFMUxXSmlabVl0T1RZd09ESTBZMlZqT0RjNE9EaGpPV1ZrTmpRdE1EWmlOQzAwTnpFM0xXRXlOVGN0TldVM09UZGtNV1E0T1RsaSIsInJleHAiOiIyMDI2LTAxLTI1VDA4OjU2OjM4WiIsInNjb3BlIjoib3BlbmlkIHByb2ZpbGUgZW1haWwiLCJzdWIiOiJVMzdMM1VqdUFsSmdISmtZR1BoM1o0bkxjZG5nIiwidG9rZW5fdHlwZSI6ImlkX3Rva2VuIn0.mifmGUi8vQmkpZYw7P9jL7C8TkucovLYmX37rrUAg00B4K7lHlo0rvPPHcjVT-R8LSQMBgitSOe4JxBk_tZDHguimMbJwT3hSlfpvNDpQpISa6-a1PyAaKyno0EzMEQZISZhWWEiCN6DSGFNzOC3EH7pBeDTKs5gCaMIvATewy_j5ajpufDGpYO2oYBcuLS-43lA9nP29pN56unGiJ5sg9ZPNlTOxsg3RY_9JzRNHzphugN4W9osBv2pfhtQPVvRWhsP6UULmk4AlqbS8Nja6VRb1jtXuqEWT76ObI7mmng2ipPJyaS1gYVTZ_FbhmW1IjVo-4uPmhvTro1ofw09_Q"",
                ""expires_in"": 600,
                ""scope"": ""openid profile email""
            }";

            // Act - Parse the JSON the same way the OIDC middleware does
            var message = ParseTokenResponseJson(tokenResponseJson);

            // Assert
            Assert.NotNull(message.IdToken);
            Assert.NotEmpty(message.IdToken);
            Assert.NotNull(message.AccessToken);
            Assert.NotEmpty(message.AccessToken);
            Assert.NotNull(message.RefreshToken);
            Assert.NotEmpty(message.RefreshToken);
            Assert.Equal("Bearer", message.TokenType);
            Assert.Equal("600", message.ExpiresIn);
            Assert.Equal("openid profile email", message.Scope);
        }

        /// <summary>
        /// Verifies that the IdToken can be decoded and contains expected claims.
        /// </summary>
        [Fact]
        public void TokenEndpointResponse_IdToken_ContainsExpectedClaims()
        {
            // Arrange
            var idToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMzZaMlg0TE93dFlVRHp5b1MwTk9ndWxQMHdoIiwidHlwIjoiSldUIn0.eyJhdWQiOlsiUDM2WjJXeFR2ZU1RQldsTGZDTVBsQzNRTTFPYSJdLCJhenAiOiJQMzZaMld4VHZlTVFCV2xMZkNNUGxDM1FNMU9hIiwiZW1haWwiOiJ5b3NpKzFAZGVzY29wZS5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiZXhwIjoxNzY2OTEyNzk4LCJpYXQiOjE3NjY5MTIxOTgsImlzcyI6Imh0dHBzOi8veW9zaS5kZXNjb3BlLnRlYW0vUDM2WjJXeFR2ZU1RQldsTGZDTVBsQzNRTTFPYSIsIm5vbmNlIjoiNjM5MDI1MDg5ODQ5OTQwMTMwLlpqWXlZemRrTVRBdFltSTNNaTAwT0dFMUxXSmlabVl0T1RZd09ESTBZMlZqT0RjNE9EaGpPV1ZrTmpRdE1EWmlOQzAwTnpFM0xXRXlOVGN0TldVM09UZGtNV1E0T1RsaSIsInJleHAiOiIyMDI2LTAxLTI1VDA4OjU2OjM4WiIsInNjb3BlIjoib3BlbmlkIHByb2ZpbGUgZW1haWwiLCJzdWIiOiJVMzdMM1VqdUFsSmdISmtZR1BoM1o0bkxjZG5nIiwidG9rZW5fdHlwZSI6ImlkX3Rva2VuIn0.mifmGUi8vQmkpZYw7P9jL7C8TkucovLYmX37rrUAg00B4K7lHlo0rvPPHcjVT-R8LSQMBgitSOe4JxBk_tZDHguimMbJwT3hSlfpvNDpQpISa6-a1PyAaKyno0EzMEQZISZhWWEiCN6DSGFNzOC3EH7pBeDTKs5gCaMIvATewy_j5ajpufDGpYO2oYBcuLS-43lA9nP29pN56unGiJ5sg9ZPNlTOxsg3RY_9JzRNHzphugN4W9osBv2pfhtQPVvRWhsP6UULmk4AlqbS8Nja6VRb1jtXuqEWT76ObI7mmng2ipPJyaS1gYVTZ_FbhmW1IjVo-4uPmhvTro1ofw09_Q";

            // Act - Decode the JWT without signature validation (we just want to read claims)
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(idToken);

            // Assert
            Assert.Equal("id_token", jwt.Payload["token_type"]?.ToString());
            Assert.Equal("yosi+1@descope.com", jwt.Payload["email"]?.ToString());
            Assert.True((bool?)jwt.Payload["email_verified"]);
            Assert.Equal("U37L3UjuAlJgHJkYGPh3Z4nLcdng", jwt.Subject);
            Assert.Contains("P36Z2WxTveMQBWlLfCMPlC3QM1Oa", jwt.Audiences);
            Assert.Equal("https://yosi.descope.team/P36Z2WxTveMQBWlLfCMPlC3QM1Oa", jwt.Issuer);
        }

        /// <summary>
        /// Verifies that a token response without an id_token field results in null IdToken.
        /// This is the negative test case to ensure our assertion is meaningful.
        /// </summary>
        [Fact]
        public void TokenEndpointResponse_WithoutIdToken_IdToken_IsNull()
        {
            // Arrange - Token response without id_token
            var tokenResponseJson = @"{
                ""access_token"": ""some_access_token"",
                ""token_type"": ""Bearer"",
                ""expires_in"": 600
            }";

            // Act
            var message = ParseTokenResponseJson(tokenResponseJson);

            // Assert
            Assert.Null(message.IdToken);
        }

        /// <summary>
        /// Verifies that OpenIdConnectMessage's string constructor correctly parses JSON.
        /// This proves that the constructor auto-detects JSON format (when string starts with '{').
        /// </summary>
        [Fact]
        public void OpenIdConnectMessage_StringConstructor_ParsesJsonCorrectly()
        {
            // Arrange - JSON token response
            var tokenResponseJson = @"{
                ""access_token"": ""test_access_token"",
                ""token_type"": ""Bearer"",
                ""id_token"": ""test_id_token"",
                ""expires_in"": 600,
                ""scope"": ""openid profile email""
            }";

            // Act - Parse using the string constructor (which detects JSON format)
            var message = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage(tokenResponseJson);

            // Assert - OpenIdConnectMessage correctly parses JSON when string starts with '{'
            Assert.NotNull(message.IdToken);
            Assert.Equal("test_id_token", message.IdToken);
            Assert.NotNull(message.AccessToken);
            Assert.Equal("test_access_token", message.AccessToken);
        }

        /// <summary>
        /// This test uses the EXACT JSON format from the runtime response to reproduce the bug.
        /// The runtime response is a single-line JSON with spaces after commas.
        /// This bug existed in Microsoft.IdentityModel.Protocols.OpenIdConnect version 7.0.3
        /// and was fixed in version 8.0.0+.
        /// </summary>
        [Fact]
        public void TokenEndpointResponse_ExactRuntimeFormat_IdToken_IsNotNull()
        {
            // Arrange - EXACT single-line JSON format from runtime response (with spaces after commas)
            var tokenResponseJson = @"{""access_token"":""test_access"", ""token_type"":""Bearer"", ""refresh_token"":""test_refresh"", ""id_token"":""test_id_token"", ""expires_in"":600, ""scope"":""openid profile email""}";

            // Act - Parse using the string constructor (which is what the OIDC middleware uses)
            var message = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage(tokenResponseJson);

            // Assert - All properties should be correctly parsed
            Assert.NotNull(message.AccessToken);
            Assert.Equal("test_access", message.AccessToken);
            Assert.NotNull(message.IdToken);
            Assert.Equal("test_id_token", message.IdToken);
            Assert.NotNull(message.RefreshToken);
            Assert.Equal("test_refresh", message.RefreshToken);
            Assert.Equal("Bearer", message.TokenType);
            Assert.Equal("600", message.ExpiresIn);
            Assert.Equal("openid profile email", message.Scope);
        }
    }
}
#endif
