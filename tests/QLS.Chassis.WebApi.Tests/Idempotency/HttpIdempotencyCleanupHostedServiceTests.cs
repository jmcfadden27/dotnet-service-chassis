using QLS.Chassis.Core.Resilience;
using QLS.Chassis.WebApi.Data;
using QLS.Chassis.WebApi.Idempotency;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Tests.Idempotency;

public sealed class HttpIdempotencyCleanupHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_LogsSuccess_WhenDeletesRows()
    {
        var logger = new RecordingStructuredAppLogger();
        var executor = new StubDatabaseExecutor(_ => Task.FromResult(3));
        var service = CreateService(executor, logger);

        await service.StartAsync(CancellationToken.None);
        var logged = await WaitUntilAsync(() => logger.SuccessCount > 0);
        await service.StopAsync(CancellationToken.None);

        Assert.True(logged);
    }

    [Fact]
    public async Task ExecuteAsync_LogsFailure_WhenDeleteThrows()
    {
        var logger = new RecordingStructuredAppLogger();
        var executor = new StubDatabaseExecutor(_ => throw new InvalidOperationException("fail"));
        var service = CreateService(executor, logger);

        await service.StartAsync(CancellationToken.None);
        var logged = await WaitUntilAsync(() => logger.FailureCount > 0);
        await service.StopAsync(CancellationToken.None);

        Assert.True(logged);
    }

    // Waits for the background loop instead of a fixed delay, so a slow machine doesn't fail the test.
    private static async Task<bool> WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                return false;
            await Task.Delay(10);
        }

        return true;
    }

    private static HttpIdempotencyCleanupHostedService CreateService(IDatabaseResilienceExecutor executor, IStructuredAppLogger logger)
    {
        return new HttpIdempotencyCleanupHostedService(
            new ChassisDatabaseOptions { ConnectionString = "Host=localhost;Database=unused" },
            new ResilienceOptions { DependencyName = "postgres", TimeoutMs = 1000 },
            new HttpIdempotencyCleanupPolicy { CleanupInterval = TimeSpan.FromMilliseconds(5), CleanupBatchSize = 10 },
            HttpIdempotencyStorePolicies.Default,
            executor,
            logger);
    }

    private sealed class StubDatabaseExecutor : IDatabaseResilienceExecutor
    {
        private readonly Func<CancellationToken, Task<int>> _operation;

        public StubDatabaseExecutor(Func<CancellationToken, Task<int>> operation)
        {
            _operation = operation;
        }

        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        {
            if (typeof(T) == typeof(int))
                return _operation(cancellationToken).ContinueWith(t => (T)(object)t.Result, cancellationToken);

            return operation(cancellationToken);
        }

        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
            => operation(cancellationToken);
    }

    private sealed class RecordingStructuredAppLogger : IStructuredAppLogger
    {
        private int _successCount;
        private int _failureCount;

        public int SuccessCount => Volatile.Read(ref _successCount);
        public int FailureCount => Volatile.Read(ref _failureCount);

        public void LogOperationSucceeded(string operation, int statusCode, long? durationMs = null, IReadOnlyDictionary<string, string?>? operationMetadata = null, string? messageId = null, string? causationId = null)
        {
            Interlocked.Increment(ref _successCount);
        }

        public void LogOperationFailed(string operation, Exception exception, int statusCode, long? durationMs = null, IReadOnlyDictionary<string, string?>? operationMetadata = null, string? messageId = null, string? causationId = null)
        {
            Interlocked.Increment(ref _failureCount);
        }

        public void LogOperationWarning(string operation, Exception exception, int statusCode, long? durationMs = null, IReadOnlyDictionary<string, string?>? operationMetadata = null, string? messageId = null, string? causationId = null)
        {
        }
    }
}
