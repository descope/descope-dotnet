using Descope.Internal.Auth;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text.Json;
using Xunit;

namespace Descope.Test.Unit
{
    public class TokenClaimsTests
    {

        [Fact]
        public void ValidatePermissions_WithRealJwtToken_ReturnsTrue()
        {
            // Arrange - Using a real JWT token with tenant permissions
            var tokenString = "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMmVYeUlnNFZCRHpiMzVCaFJFRnZWTnB2cFU1IiwidHlwIjoiSldUIn0.eyJhbXIiOlsib2F1dGgiXSwiYXVkIjpbIlAyZVh5SWg3bEE3WGRWYXREUUhsZ0Q0NTV6WGQiXSwiZHJuIjoiRFMiLCJleHAiOjE3NjQxNjA3MjIsImlhdCI6MTc2NDE1NzEyMiwiaXNzIjoiaHR0cHM6Ly9hcGkuZGVzY29wZS5jb20vUDJlWHlJaDdsQTdYZFZhdERRSGxnRDQ1NXpYZCIsInJleHAiOiIyMDI1LTEyLTAxVDExOjM4OjQyWiIsInN1YiI6IlUzMXJ6MTVrS1ZZd0NsakFOWDlOREFxMm5ySUkiLCJ0ZW5hbnRzIjp7IlQzMlZPUnlHeFhENzh0bFhUWUhoSHlCZGplZ3MiOnsicGVybWlzc2lvbnMiOlsiVXNlciBBZG1pbiIsIlNTTyBBZG1pbiIsIkltcGVyc29uYXRlIl0sInJvbGVzIjpbIlRlbmFudCBBZG1pbiJdfSwieHh4Ijp7InBlcm1pc3Npb25zIjpbIlVzZXIgQWRtaW4iLCJTU08gQWRtaW4iLCJJbXBlcnNvbmF0ZSJdLCJyb2xlcyI6WyJUZW5hbnQgQWRtaW4iXX19fQ.x1o7ZXVIbskHHimBQFU9Oep6vb919NP8rgTDZDYMwDxcSCFLz6f0Kvf0Iqz9Rxcmf6CimC8rtzpqc3LFJumBbkzbzGvLRFVKYN40kEKnkRxiZTX9qGfwgsvf9h03yPbiUwuiy0IZgU4grOe56Zg7poMYkJbL2iN5JIwnpB-HoJUb82L_Jh-Odrl448v1QLKLz1varISVASzz1H_vkwXgnSZR4R3G366db7z4TkXoivWvojq_3iUPHI9FxxyXd7WnXlkrXrGASUgxluoJEFh_s7tqDNn0y3hCaVN1g1sswwQccpqjfSoVbX-UG8ppk5cxZY7bEiynUp7Yf2K47xL7JQ";
            var jsonWebToken = new JsonWebToken(tokenString);
            var descopeToken = new Token(jsonWebToken);
            var auth = new Authentication(new MockHttpClient());
            var permissionsToCheck = new List<string> { "SSO Admin", "User Admin", "Impersonate" };

            // Act
            var result = auth.ValidatePermissions(descopeToken, permissionsToCheck, "xxx");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Token_ParsesTenantsClaimCorrectly()
        {
            // Arrange - JWT with tenants claim containing permissions and roles
            var tokenString = "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMmVYeUlnNFZCRHpiMzVCaFJFRnZWTnB2cFU1IiwidHlwIjoiSldUIn0.eyJhbXIiOlsib2F1dGgiXSwiYXVkIjpbIlAyZVh5SWg3bEE3WGRWYXREUUhsZ0Q0NTV6WGQiXSwiZHJuIjoiRFMiLCJleHAiOjE3NjQxNjA3MjIsImlhdCI6MTc2NDE1NzEyMiwiaXNzIjoiaHR0cHM6Ly9hcGkuZGVzY29wZS5jb20vUDJlWHlJaDdsQTdYZFZhdERRSGxnRDQ1NXpYZCIsInJleHAiOiIyMDI1LTEyLTAxVDExOjM4OjQyWiIsInN1YiI6IlUzMXJ6MTVrS1ZZd0NsakFOWDlOREFxMm5ySUkiLCJ0ZW5hbnRzIjp7IlQzMlZPUnlHeFhENzh0bFhUWUhoSHlCZGplZ3MiOnsicGVybWlzc2lvbnMiOlsiVXNlciBBZG1pbiIsIlNTTyBBZG1pbiIsIkltcGVyc29uYXRlIl0sInJvbGVzIjpbIlRlbmFudCBBZG1pbiJdfSwieHh4Ijp7InBlcm1pc3Npb25zIjpbIlVzZXIgQWRtaW4iLCJTU08gQWRtaW4iLCJJbXBlcnNvbmF0ZSJdLCJyb2xlcyI6WyJUZW5hbnQgQWRtaW4iXX19fQ.x1o7ZXVIbskHHimBQFU9Oep6vb919NP8rgTDZDYMwDxcSCFLz6f0Kvf0Iqz9Rxcmf6CimC8rtzpqc3LFJumBbkzbzGvLRFVKYN40kEKnkRxiZTX9qGfwgsvf9h03yPbiUwuiy0IZgU4grOe56Zg7poMYkJbL2iN5JIwnpB-HoJUb82L_Jh-Odrl448v1QLKLz1varISVASzz1H_vkwXgnSZR4R3G366db7z4TkXoivWvojq_3iUPHI9FxxyXd7WnXlkrXrGASUgxluoJEFh_s7tqDNn0y3hCaVN1g1sswwQccpqjfSoVbX-UG8ppk5cxZY7bEiynUp7Yf2K47xL7JQ";
            var jsonWebToken = new JsonWebToken(tokenString);

            // Act
            var token = new Token(jsonWebToken);
            var tenants = token.GetTenants();

            // Assert
            Assert.Equal(2, tenants.Count);
            Assert.Contains("T32VORyGxXD78tlXTYHhHyBdjegs", tenants);
            Assert.Contains("xxx", tenants);

            // Verify permissions are parsed as List<string>
            var permissions = token.GetTenantValue("xxx", "permissions") as List<string>;
            Assert.NotNull(permissions);
            Assert.Equal(3, permissions.Count);
            Assert.Contains("User Admin", permissions);
            Assert.Contains("SSO Admin", permissions);
            Assert.Contains("Impersonate", permissions);

            // Verify roles are parsed as List<string>
            var roles = token.GetTenantValue("xxx", "roles") as List<string>;
            Assert.NotNull(roles);
            Assert.Single(roles);
            Assert.Contains("Tenant Admin", roles);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesStringArray()
        {
            // Arrange
            var json = @"[""value1"", ""value2"", ""value3""]";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            var list = Assert.IsType<List<string>>(result);
            Assert.Equal(3, list.Count);
            Assert.Contains("value1", list);
            Assert.Contains("value2", list);
            Assert.Contains("value3", list);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesMixedArray()
        {
            // Arrange
            var json = @"[""string"", 123, true, null]";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            var list = Assert.IsType<List<object>>(result);
            Assert.Equal(4, list.Count);
            Assert.Equal("string", list[0]);
            Assert.Equal(123, list[1]);
            Assert.Equal(true, list[2]);
            Assert.Null(list[3]);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesNestedObject()
        {
            // Arrange
            var json = @"{""outer"": {""inner"": ""value"", ""number"": 42}}";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            var dict = Assert.IsType<Dictionary<string, object>>(result);
            Assert.Single(dict);
            var outerDict = Assert.IsType<Dictionary<string, object>>(dict["outer"]);
            Assert.Equal("value", outerDict["inner"]);
            Assert.Equal(42, outerDict["number"]);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesString()
        {
            // Arrange
            var json = @"""test string""";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            Assert.Equal("test string", result);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesInteger()
        {
            // Arrange
            var json = "42";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesLong()
        {
            // Arrange
            var json = "9223372036854775807"; // Max long value
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            Assert.Equal(9223372036854775807L, result);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesDouble()
        {
            // Arrange
            var json = "3.14159";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            Assert.Equal(3.14159, result);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesTrue()
        {
            // Arrange
            var json = "true";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            Assert.Equal(true, result);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesFalse()
        {
            // Arrange
            var json = "false";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            Assert.Equal(false, result);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesNull()
        {
            // Arrange
            var json = "null";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Token_ConvertJsonElement_HandlesComplexNestedStructure()
        {
            // Arrange - Simulating a tenants claim structure
            var json = @"{
                ""tenant1"": {
                    ""permissions"": [""read"", ""write"", ""delete""],
                    ""roles"": [""admin"", ""user""],
                    ""metadata"": {
                        ""active"": true,
                        ""count"": 5,
                        ""ratio"": 0.75
                    }
                },
                ""tenant2"": {
                    ""permissions"": [""read""],
                    ""roles"": []
                }
            }";
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            // Act
            var result = InvokeConvertJsonElement(element);

            // Assert
            var dict = Assert.IsType<Dictionary<string, object>>(result);
            Assert.Equal(2, dict.Count);

            var tenant1 = Assert.IsType<Dictionary<string, object>>(dict["tenant1"]);
            var permissions = Assert.IsType<List<string>>(tenant1["permissions"]);
            Assert.Equal(3, permissions.Count);
            Assert.Contains("read", permissions);

            var metadata = Assert.IsType<Dictionary<string, object>>(tenant1["metadata"]);
            Assert.Equal(true, metadata["active"]);
            Assert.Equal(5, metadata["count"]);
            Assert.Equal(0.75, metadata["ratio"]);
        }

        [Fact]
        public void Token_GetTenants_ReturnsEmptyList_WhenNoTenantsClaim()
        {
            // Arrange - Simple JWT without tenants claim
            var claims = new Dictionary<string, object>
            {
                { "sub", "user123" },
                { "iss", "https://api.descope.com/projectId" }
            };
            var token = new Token("jwt", "user123", "projectId", DateTime.UtcNow, claims);

            // Act
            var tenants = token.GetTenants();

            // Assert
            Assert.Empty(tenants);
        }

        [Fact]
        public void Token_GetTenantValue_ReturnsNull_WhenTenantNotFound()
        {
            // Arrange
            var tokenString = "eyJhbGciOiJSUzI1NiIsImtpZCI6IlNLMmVYeUlnNFZCRHpiMzVCaFJFRnZWTnB2cFU1IiwidHlwIjoiSldUIn0.eyJhbXIiOlsib2F1dGgiXSwiYXVkIjpbIlAyZVh5SWg3bEE3WGRWYXREUUhsZ0Q0NTV6WGQiXSwiZHJuIjoiRFMiLCJleHAiOjE3NjQxNjA3MjIsImlhdCI6MTc2NDE1NzEyMiwiaXNzIjoiaHR0cHM6Ly9hcGkuZGVzY29wZS5jb20vUDJlWHlJaDdsQTdYZFZhdERRSGxnRDQ1NXpYZCIsInJleHAiOiIyMDI1LTEyLTAxVDExOjM4OjQyWiIsInN1YiI6IlUzMXJ6MTVrS1ZZd0NsakFOWDlOREFxMm5ySUkiLCJ0ZW5hbnRzIjp7IlQzMlZPUnlHeFhENzh0bFhUWUhoSHlCZGplZ3MiOnsicGVybWlzc2lvbnMiOlsiVXNlciBBZG1pbiIsIlNTTyBBZG1pbiIsIkltcGVyc29uYXRlIl0sInJvbGVzIjpbIlRlbmFudCBBZG1pbiJdfSwieHh4Ijp7InBlcm1pc3Npb25zIjpbIlVzZXIgQWRtaW4iLCJTU08gQWRtaW4iLCJJbXBlcnNvbmF0ZSJdLCJyb2xlcyI6WyJUZW5hbnQgQWRtaW4iXX19fQ.x1o7ZXVIbskHHimBQFU9Oep6vb919NP8rgTDZDYMwDxcSCFLz6f0Kvf0Iqz9Rxcmf6CimC8rtzpqc3LFJumBbkzbzGvLRFVKYN40kEKnkRxiZTX9qGfwgsvf9h03yPbiUwuiy0IZgU4grOe56Zg7poMYkJbL2iN5JIwnpB-HoJUb82L_Jh-Odrl448v1QLKLz1varISVASzz1H_vkwXgnSZR4R3G366db7z4TkXoivWvojq_3iUPHI9FxxyXd7WnXlkrXrGASUgxluoJEFh_s7tqDNn0y3hCaVN1g1sswwQccpqjfSoVbX-UG8ppk5cxZY7bEiynUp7Yf2K47xL7JQ";
            var jsonWebToken = new JsonWebToken(tokenString);
            var token = new Token(jsonWebToken);

            // Act
            var result = token.GetTenantValue("nonexistent-tenant", "permissions");

            // Assert
            Assert.Null(result);
        }

        // Helper method to invoke the private ConvertJsonElement method via reflection
        // This allows us to test the method directly
        private static object InvokeConvertJsonElement(JsonElement element)
        {
            var tokenType = typeof(Token);
            var method = tokenType.GetMethod("ConvertJsonElement",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (method == null)
            {
                throw new InvalidOperationException("ConvertJsonElement method not found");
            }

            var result = method.Invoke(null, new object[] { element });
            return result!;
        }
    }
}
