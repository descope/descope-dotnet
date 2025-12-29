namespace Descope.Test.Integration
{
    /// <summary>
    /// Base class for integration tests that enforces rate limiting between tests
    /// to prevent hitting Descope API rate limits in CI.
    /// XUnit creates a new instance of the test class for each test method,
    /// so the constructor and Dispose are called for every test.
    /// </summary>
    public abstract class RateLimitedIntegrationTest : IDisposable
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static DateTime _lastTestEndTime = DateTime.MinValue;

        protected readonly int extraSleepTime = GetDelayBasedOnPlatform();

        // Delay between tests in milliseconds
        // 1000ms provides ~60 requests per 60 seconds (buffer below the 100 req/60s limit since each test may make multiple requests)
        // Set to 0 on macOS for faster local development, 1000ms in CI to respect rate limits
        private static readonly int DelayBetweenTestsMs = GetDelayBasedOnPlatform();

        protected static int GetDelayBasedOnPlatform()
        {
            return OperatingSystem.IsMacOS() ? 0 : 1000; // We do not need rate limiting on macOS for local dev
        }

        protected RateLimitedIntegrationTest()
        {
            _semaphore.Wait();

            var timeSinceLastTest = DateTime.UtcNow - _lastTestEndTime;
            var requiredDelay = TimeSpan.FromMilliseconds(DelayBetweenTestsMs);

            if (timeSinceLastTest < requiredDelay)
            {
                var remainingDelay = requiredDelay - timeSinceLastTest;
                Thread.Sleep(remainingDelay);
            }
        }

        public void Dispose()
        {
            _lastTestEndTime = DateTime.UtcNow;
            _semaphore.Release();
            GC.SuppressFinalize(this);
        }

        protected async Task RetryUntilSuccessAsync(Func<Task> assertion, int timeoutSeconds = 6)
        {
            var endTime = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            Exception? lastException = null;

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    await assertion();
                    return; // Success!
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    await Task.Delay(500); // Wait 500ms before retry
                }
            }

            // If we get here, all retries failed
            throw lastException ?? new TimeoutException($"Assertion failed after {timeoutSeconds} seconds");
        }
    }
}
