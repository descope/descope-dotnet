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
        public void Token_ParsesTenantsWithEmptyArrays()
        {
            // Arrange - Tenant with empty roles and permissions arrays
            var json = @"{""tenant1"": {""roles"": [], ""permissions"": []}}";
            var claims = new Dictionary<string, object>
            {
                { "sub", "user123" },
                { "iss", "https://api.descope.com/projectId" },
                { "tenants", json }
            };
            var token = new Token("jwt", "user123", "projectId", DateTime.UtcNow, claims);

            // Act
            var tenants = token.GetTenants();
            var roles = token.GetTenantValue("tenant1", "roles") as List<string>;
            var permissions = token.GetTenantValue("tenant1", "permissions") as List<string>;

            // Assert
            Assert.Single(tenants);
            Assert.Contains("tenant1", tenants);
            Assert.NotNull(roles);
            Assert.Empty(roles);
            Assert.NotNull(permissions);
            Assert.Empty(permissions);
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
    }
}
