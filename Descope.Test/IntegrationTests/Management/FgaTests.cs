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
            try
            {
                // Save schema with simple DSL
                var saveSchemaRequest = new SaveDSLSchemaRequest
                {
                    Dsl = SimpleFgaSchema
                };
                await _descopeClient.Mgmt.V1.Fga.Schema.PostAsync(saveSchemaRequest);

                // Create a relation u1->u2
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
                await _descopeClient.Mgmt.V1.Fga.Relations.PostAsync(createRelationsRequest);

                // Check that the relation exists
                await RetryUntilSuccessAsync(async () =>
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
                    var checkResponse = await _descopeClient.Mgmt.V1.Fga.Check.PostAsync(checkRequest);

                    // Verify the relation was found
                    Assert.NotNull(checkResponse);
                    Assert.NotNull(checkResponse.Tuples);
                    Assert.Single(checkResponse.Tuples);
                    Assert.True(checkResponse.Tuples[0].Allowed, "Expected relation u1->u2 to be allowed");
                });
            }
            finally
            {
                // Delete the relations (cleanup, but also verifies DeleteRelations works)
                await _descopeClient.Mgmt.V1.Fga.Relations.DeleteAsync();
                // Check that the relation no longer exists
                await RetryUntilSuccessAsync(async () =>
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
                    var checkResponse = await _descopeClient.Mgmt.V1.Fga.Check.PostAsync(checkRequest);

                    // Verify the relation was found
                    Assert.NotNull(checkResponse);
                    Assert.NotNull(checkResponse.Tuples);
                    Assert.Single(checkResponse.Tuples);
                    Assert.False(checkResponse.Tuples[0].Allowed, "Expected relation u1->u2 to be deleted");
                });
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
