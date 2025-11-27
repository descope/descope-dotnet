using Descope.Internal.Auth;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Cryptography;
using Xunit;

namespace Descope.Test.Unit
{
    public class AuthorizationTests
    {
        // Helper method to create a test JWT with custom claims
        private string CreateTestJwt(Dictionary<string, object> claims)
        {
            // Create a simple JWT for testing
            // Note: This is a simplified version - in production, JWTs should be properly signed
            var header = new Dictionary<string, object>
            {
                { "alg", "RS256" },
                { "typ", "JWT" },
                { "kid", "test-key" }
            };

            var payload = new Dictionary<string, object>(claims)
            {
                { "iss", "https://api.descope.com/P123" },
                { "sub", "test-user" },
                { "exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() },
                { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            };

            var headerJson = System.Text.Json.JsonSerializer.Serialize(header);
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

            var headerBase64 = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(headerJson));
            var payloadBase64 = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(payloadJson));

            // For testing purposes, use a dummy signature
            var signatureBase64 = "dummy-signature";

            return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
        }

        private string Base64UrlEncode(byte[] input)
        {
            var base64 = Convert.ToBase64String(input);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        [Fact]
        public void Token_ParsesSimpleClaims()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "name", "Test User" },
                { "email", "test@example.com" }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);

            // Act
            var token = new Token(jsonWebToken);

            // Assert
            Assert.Equal("Test User", token.Claims["name"]);
            Assert.Equal("test@example.com", token.Claims["email"]);
        }

        [Fact]
        public void Token_ParsesArrayClaims()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "permissions", new[] { "read", "write" } },
                { "roles", new[] { "admin", "user" } }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);

            // Act
            var token = new Token(jsonWebToken);

            // Assert
            Assert.True(token.Claims.ContainsKey("permissions"));
            Assert.True(token.Claims.ContainsKey("roles"));
            
            // The claims should be deserialized as List<object>
            var permissions = token.Claims["permissions"];
            var roles = token.Claims["roles"];
            
            Assert.NotNull(permissions);
            Assert.NotNull(roles);
        }

        [Fact]
        public void Token_ParsesNestedTenantClaims()
        {
            // Arrange - This is the key test for the bug fix
            var claims = new Dictionary<string, object>
            {
                { "tenants", new Dictionary<string, object>
                    {
                        { "tenant1", new Dictionary<string, object>
                            {
                                { "permissions", new[] { "read", "write", "delete" } },
                                { "roles", new[] { "admin" } }
                            }
                        },
                        { "tenant2", new Dictionary<string, object>
                            {
                                { "permissions", new[] { "read" } },
                                { "roles", new[] { "viewer" } }
                            }
                        }
                    }
                }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);

            // Act
            var token = new Token(jsonWebToken);

            // Assert
            Assert.True(token.Claims.ContainsKey("tenants"));
            var tenants = token.Claims["tenants"] as Dictionary<string, object>;
            Assert.NotNull(tenants);
            Assert.Equal(2, tenants.Count);
            Assert.True(tenants.ContainsKey("tenant1"));
            Assert.True(tenants.ContainsKey("tenant2"));

            // Verify tenant1 structure
            var tenant1 = tenants["tenant1"] as Dictionary<string, object>;
            Assert.NotNull(tenant1);
            Assert.True(tenant1.ContainsKey("permissions"));
            Assert.True(tenant1.ContainsKey("roles"));
        }

        [Fact]
        public void ValidatePermissions_WithTenant_ReturnsTrue()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "tenants", new Dictionary<string, object>
                    {
                        { "tenant1", new Dictionary<string, object>
                            {
                                { "permissions", new[] { "read", "write", "delete" } },
                                { "roles", new[] { "admin" } }
                            }
                        }
                    }
                }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);
            var token = new Token(jsonWebToken);
            var auth = new Authentication(new MockHttpClient());

            // Act
            var hasReadPermission = auth.ValidatePermissions(token, new List<string> { "read" }, "tenant1");
            var hasWritePermission = auth.ValidatePermissions(token, new List<string> { "write" }, "tenant1");
            var hasMultiplePermissions = auth.ValidatePermissions(token, new List<string> { "read", "write" }, "tenant1");

            // Assert
            Assert.True(hasReadPermission);
            Assert.True(hasWritePermission);
            Assert.True(hasMultiplePermissions);
        }

        [Fact]
        public void ValidatePermissions_WithTenant_ReturnsFalseForMissingPermission()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "tenants", new Dictionary<string, object>
                    {
                        { "tenant1", new Dictionary<string, object>
                            {
                                { "permissions", new[] { "read" } }
                            }
                        }
                    }
                }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);
            var token = new Token(jsonWebToken);
            var auth = new Authentication(new MockHttpClient());

            // Act
            var hasWritePermission = auth.ValidatePermissions(token, new List<string> { "write" }, "tenant1");

            // Assert
            Assert.False(hasWritePermission);
        }

        [Fact]
        public void ValidatePermissions_WithoutTenant_ReturnsTrue()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "permissions", new[] { "global-read", "global-write" } }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);
            var token = new Token(jsonWebToken);
            var auth = new Authentication(new MockHttpClient());

            // Act
            var hasPermission = auth.ValidatePermissions(token, new List<string> { "global-read" }, null);

            // Assert
            Assert.True(hasPermission);
        }

        [Fact]
        public void ValidateRoles_WithTenant_ReturnsTrue()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "tenants", new Dictionary<string, object>
                    {
                        { "tenant1", new Dictionary<string, object>
                            {
                                { "roles", new[] { "admin", "user" } }
                            }
                        }
                    }
                }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);
            var token = new Token(jsonWebToken);
            var auth = new Authentication(new MockHttpClient());

            // Act
            var hasAdminRole = auth.ValidateRoles(token, new List<string> { "admin" }, "tenant1");
            var hasUserRole = auth.ValidateRoles(token, new List<string> { "user" }, "tenant1");

            // Assert
            Assert.True(hasAdminRole);
            Assert.True(hasUserRole);
        }

        [Fact]
        public void GetMatchedPermissions_WithTenant_ReturnsMatchedList()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "tenants", new Dictionary<string, object>
                    {
                        { "tenant1", new Dictionary<string, object>
                            {
                                { "permissions", new[] { "read", "write" } }
                            }
                        }
                    }
                }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);
            var token = new Token(jsonWebToken);
            var auth = new Authentication(new MockHttpClient());

            // Act
            var matched = auth.GetMatchedPermissions(token, new List<string> { "read", "delete", "write" }, "tenant1");

            // Assert
            Assert.Equal(2, matched.Count);
            Assert.Contains("read", matched);
            Assert.Contains("write", matched);
            Assert.DoesNotContain("delete", matched);
        }

        [Fact]
        public void GetMatchedRoles_WithTenant_ReturnsMatchedList()
        {
            // Arrange
            var claims = new Dictionary<string, object>
            {
                { "tenants", new Dictionary<string, object>
                    {
                        { "tenant1", new Dictionary<string, object>
                            {
                                { "roles", new[] { "admin" } }
                            }
                        }
                    }
                }
            };
            var jwtString = CreateTestJwt(claims);
            var jsonWebToken = new JsonWebToken(jwtString);
            var token = new Token(jsonWebToken);
            var auth = new Authentication(new MockHttpClient());

            // Act
            var matched = auth.GetMatchedRoles(token, new List<string> { "admin", "user", "guest" }, "tenant1");

            // Assert
            Assert.Single(matched);
            Assert.Contains("admin", matched);
        }
    }
}
