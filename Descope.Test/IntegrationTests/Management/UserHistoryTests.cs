using Xunit;
using Descope.Mgmt.Models.Managementv1;

namespace Descope.Test.Integration
{
    public class UserHistoryTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task UserHistory_LoadUsersAuthHistory()
        {
            string? userId = null;
            try
            {
                // Create a test user
                var userName = Guid.NewGuid().ToString();
                var createUserRequest = new CreateUserRequest
                {
                    Identifier = userName,
                    Email = userName + "@test.com",
                    VerifiedEmail = true
                };
                var userResponse = await _descopeClient.Mgmt.V1.User.Create.PostAsync(createUserRequest);
                userId = userResponse?.User?.UserId;
                Assert.NotNull(userId);

                // Load users auth history using the endpoint with response_body: "usersAuthHistory"
                var userAuthHistoryRequest = new Descope.Mgmt.Models.Managementv1.UsersAuthHistoryRequest
                {
                    UserIds = new List<string> { userId }
                };
                var historyResponse = await _descopeClient.Mgmt.V2.User.History.PostAsync(userAuthHistoryRequest);

                Assert.NotNull(historyResponse);
                // History might be empty for a newly created user, which is fine
                // The important part is that the request succeeds and returns a valid response
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        await _descopeClient.Mgmt.V1.User.DeletePath.PostAsync(new DeleteUserRequest { Identifier = userId });
                    }
                    catch { }
                }
            }
        }
    }
}
