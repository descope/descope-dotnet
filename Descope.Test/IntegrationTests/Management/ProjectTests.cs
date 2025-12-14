using Xunit;
using Descope.Mgmt.Models.Managementv1;
using Xunit.Abstractions;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class ProjectTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();
        private readonly ITestOutputHelper _output;

        public ProjectTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Flaky when run in CI")]
        public async Task Project_Rename()
        {
            var originalName = "Dotnet SDK Testing";
            try
            {
                // Rename to a new name
                var newName = Guid.NewGuid().ToString().Split("-").First();
                var renameRequest = new UpdateProjectNameRequest
                {
                    Name = newName
                };
                await _descopeClient.Mgmt.V1.Project.Update.Name.PostAsync(renameRequest);

                // Rename back to original name
                var restoreRequest = new UpdateProjectNameRequest
                {
                    Name = originalName
                };
                await _descopeClient.Mgmt.V1.Project.Update.Name.PostAsync(restoreRequest);
            }
            finally
            {
                // Make sure we restore the original name
                try
                {
                    var restoreRequest = new UpdateProjectNameRequest
                    {
                        Name = originalName
                    };
                    await _descopeClient.Mgmt.V1.Project.Update.Name.PostAsync(restoreRequest);
                }
                catch { }
            }
        }
    }
}
