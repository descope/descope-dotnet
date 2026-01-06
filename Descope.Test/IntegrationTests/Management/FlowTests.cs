using Xunit;
using Descope.Mgmt.Models.Orchestrationv1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class FlowTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        [Fact]
        public async Task Flow_RunManagement_NonExistentFlow()
        {
            // This test demonstrates how to run a management flow.
            // We use a random flowId that doesn't exist, so the call will fail,
            // but it shows the correct way to structure the request.

            var request = new RunManagementFlowRequest
            {
                FlowId = "flow-id-" + Guid.NewGuid().ToString("N"), // Random flowId to ensure it doesn't exist
                Options = new ManagementFlowOptions
                {
                    Input = new ManagementFlowOptions_input
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "email", "name@example.com" },
                            { "customParam", "customValue" }
                        }
                    }
                }
            };
            // The call will throw an exception because the flowId doesn't exist,
            // but this demonstrates the correct usage pattern
            var exception = await Assert.ThrowsAsync<DescopeException>(async () =>
            {
                await _descopeClient.Mgmt.V1.Flow.Run.PostAsync(request);
            });

            // Verify that we got an error (since the flow doesn't exist)
            Assert.NotNull(exception);
            Assert.Contains("Failed getting flow", exception.Message);

            // ============================================================================
            // For demonstration, this is how you would normally check the response if the flow existed.
            // Commented out since the flow doesn't exist in this test environment.
            // ============================================================================
            // var response = await _descopeClient.Mgmt.V1.Flow.Run.PostAsync(request);
            // Assert.NotNull(response);
            // Assert.NotNull(response.Output);
            // var email = response.Output.AdditionalData != null && response.Output.AdditionalData.TryGetValue("EMAIL", out var emailObj)
            //     ? emailObj as string
            //     : null;
            // Assert.NotNull(email);
            // Assert.Equal("name@example.com", email);
            // ============================================================================
        }
    }
}
