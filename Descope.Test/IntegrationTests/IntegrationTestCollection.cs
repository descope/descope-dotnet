using Xunit;

namespace Descope.Test.Integration
{
    /// <summary>
    /// Collection definition for integration tests that enforces sequential execution.
    /// Combined with RateLimitedIntegrationTest base class, this ensures tests run
    /// one at a time with appropriate delays to prevent hitting Descope API rate limits.
    /// </summary>
    [CollectionDefinition("Integration Tests")]
    public class IntegrationTestCollection
    {
        // This class is never instantiated. It exists only to define the collection
        // and ensure all integration tests in this collection run sequentially.
    }
}
