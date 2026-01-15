using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;
using Xunit;
using Xunit.Abstractions;
using Descope.Mgmt.Models.Orchestrationv1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class FlowTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();
        private readonly ITestOutputHelper _output;

        public FlowTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Flow_RunManagement_NonExistentFlow()
        {
            // This test demonstrates how to run a management flow.
            // We use a random flowId that doesn't exist, so the call will fail,
            // but it shows the correct way to structure the request.

            var request = new RunManagementFlowRequest
            {
                FlowId = "mgmt-return-email" + Guid.NewGuid().ToString("N"), // Non-existent flowId
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
                await _descopeClient.Mgmt.V1.Flow.Run.PostWithJsonOutputAsync(request);
            });

            // Verify that we got an error (since the flow doesn't exist)
            Assert.NotNull(exception);
            Assert.Contains("Failed getting flow", exception.Message);

            // ============================================================================
            // For demonstration, this is how you would normally check the response if the flow existed.
            // Commented out since the flow doesn't exist in all test environments.
            // ============================================================================
            // var response = await _descopeClient.Mgmt.V1.Flow.Run.PostWithJsonOutputAsync(request);
            // Assert.NotNull(response);
            // Assert.NotNull(response.OutputJson);

            // // Access JSON properties directly using JsonDocument
            // var root = response.OutputJson!.RootElement;
            // var email = root.GetProperty("email").GetString();
            // Assert.NotNull(email);
            // Assert.Equal("name@example.com", email);

            // // Access nested objects using standard JsonDocument methods
            // var greeting = root.GetProperty("obj").GetProperty("greeting").GetString();
            // Assert.Equal("Hello, World!", greeting);
            // ============================================================================
        }
    }
}
