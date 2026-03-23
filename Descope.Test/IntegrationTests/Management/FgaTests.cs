using Xunit;
using Descope.Mgmt.Models.Authzv1;

namespace Descope.Test.Integration
{
    [Collection("Integration Tests")]
    public class FgaTests : RateLimitedIntegrationTest
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();

        private const string SimpleFgaSchema = @"model AuthZ 1.0

type User
  relation friend: User";

        [Fact]
        public async Task FgaTest()
        {
            // Use unique resource names to avoid conflicts with concurrent runs across framework versions
            var resource = Guid.NewGuid().ToString();
            var target = Guid.NewGuid().ToString();
            var tuple = new TupleObject
            {
                ResourceType = "User",
                Resource = resource,
                Relation = "friend",
                TargetType = "User",
                Target = target
            };

            // Delay test to avoid hitting rate limits in CI
            await Task.Delay(extraSleepTime * 3);

            // Save schema with simple DSL
            await _descopeClient.Mgmt.V1.Fga.Schema.PostAsync(new SaveDSLSchemaRequest { Dsl = SimpleFgaSchema });

            // Delay to allow schema to propagate before creating relations
            await Task.Delay(extraSleepTime * 3);

            try
            {
                // Create the relation
                await _descopeClient.Mgmt.V1.Fga.Relations.PostAsync(new CreateTuplesRequest
                {
                    Tuples = new List<TupleObject> { tuple }
                });

                // Wait for eventual consistency in CI
                await Task.Delay(extraSleepTime * 3);

                // Check that the relation exists
                await RetryUntilSuccessAsync(async () =>
                {
                    var checkResponse = await _descopeClient.Mgmt.V1.Fga.Check.PostAsync(new CheckRequest
                    {
                        Tuples = new List<TupleObject> { tuple }
                    });
                    Assert.NotNull(checkResponse);
                    Assert.NotNull(checkResponse.Tuples);
                    Assert.Single(checkResponse.Tuples);
                    Assert.True(checkResponse.Tuples[0].Allowed, $"Expected relation {resource}->{target} to be allowed");
                });

                // Delete only the specific relation (verifies DeleteRelations works, without affecting other concurrent runs)
                await _descopeClient.Mgmt.V1.Fga.Relations.DeletePath.PostAsync(new DeleteTuplesRequest
                {
                    Tuples = new List<TupleObject> { tuple }
                });

                // Wait for eventual consistency in CI
                await Task.Delay(extraSleepTime * 3);

                // Check that the relation no longer exists
                await RetryUntilSuccessAsync(async () =>
                {
                    var checkResponse = await _descopeClient.Mgmt.V1.Fga.Check.PostAsync(new CheckRequest
                    {
                        Tuples = new List<TupleObject> { tuple }
                    });
                    Assert.NotNull(checkResponse);
                    Assert.NotNull(checkResponse.Tuples);
                    Assert.Single(checkResponse.Tuples);
                    Assert.False(checkResponse.Tuples[0].Allowed, $"Expected relation {resource}->{target} to be deleted");
                });
            }
            finally
            {
                // Best-effort cleanup of the specific relation
                try
                {
                    await _descopeClient.Mgmt.V1.Fga.Relations.DeletePath.PostAsync(new DeleteTuplesRequest
                    {
                        Tuples = new List<TupleObject> { tuple }
                    });
                }
                catch { }
            }
        }

        [Fact(Skip = "Manual test - set FgaCacheUrl to inspect network traffic")]
        //[Fact]
        public async Task FgaCacheProxyTest()
        {
            // Create a client with FgaCacheUrl set
            var options = IntegrationTestSetup.GetDescopeClientOptions();
            options.FgaCacheUrl = "http://fga-cache.example.com/"; // Set to desired cache URL
            var clientWithCache = DescopeManagementClientFactory.Create(options);

            // Try SaveSchema
            try
            {
                var saveSchemaRequest = new SaveDSLSchemaRequest
                {
                    Dsl = SimpleFgaSchema
                };
                await clientWithCache.Mgmt.V1.Fga.Schema.PostAsync(saveSchemaRequest);
            }
            catch
            {
                // Ignore errors, we're just checking network routing
            }

            // Try CreateRelations
            try
            {
                var createRelationsRequest = new CreateTuplesRequest
                {
                    Tuples = new List<TupleObject>
                        {
                            new TupleObject
                            {
                                ResourceType = "User",
                                Resource = "u1",
                                Relation = "friend",
                                TargetType = "User",
                                Target = "u2"
                            }
                        }
                };
                await clientWithCache.Mgmt.V1.Fga.Relations.PostAsync(createRelationsRequest);
            }
            catch
            {
                // Ignore errors, we're just checking network routing
            }

            // Try Check
            try
            {
                var checkRequest = new CheckRequest
                {
                    Tuples = new List<TupleObject>
                        {
                            new TupleObject
                            {
                                ResourceType = "User",
                                Resource = "u1",
                                Relation = "friend",
                                TargetType = "User",
                                Target = "u2"
                            }
                        }
                };
                await clientWithCache.Mgmt.V1.Fga.Check.PostAsync(checkRequest);
            }
            catch
            {
                // Ignore errors, we're just checking network routing
            }

            // Try DeleteRelations
            try
            {
                var deleteRelationsRequest = new DeleteTuplesRequest
                {
                    Tuples = new List<TupleObject>
                        {
                            new TupleObject
                            {
                                ResourceType = "User",
                                Resource = "u1",
                                Relation = "friend",
                                TargetType = "User",
                                Target = "u2"
                            }
                        }
                };
                await clientWithCache.Mgmt.V1.Fga.Relations.DeletePath.PostAsync(deleteRelationsRequest);
            }
            catch
            {
                // Ignore errors, we're just checking network routing
            }
            // Try delete
            try
            {
                await clientWithCache.Mgmt.V1.Fga.Relations.DeleteAsync();
            }
            catch
            {
                // Ignore errors, we're just checking network routing
            }
            // Try non-fga cache methods
            try
            {
                await clientWithCache.Mgmt.V1.Fga.Schema.GetAsync();
            }
            catch
            {
                // Ignore errors, we're just checking network routing
            }
            // Try non-fga cache methods
            try
            {
                await clientWithCache.Mgmt.V2.User.Search.PostAsync(new Mgmt.Models.Managementv1.SearchUsersRequest());
            }
            catch
            {
                // Ignore errors, we're just checking network routing
            }
        }
    }
}
