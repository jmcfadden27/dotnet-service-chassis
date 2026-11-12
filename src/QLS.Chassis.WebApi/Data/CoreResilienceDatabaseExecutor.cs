using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.WebApi.Data;

/// <summary>
/// <see cref="IDatabaseResilienceExecutor"/> that runs operations through QLS.Chassis.Core's
/// <see cref="IResilienceExecutor"/> with the registered <see cref="ResilienceOptions"/>.
/// </summary>
/// <remarks>
/// A circuit-open failure from the resilience executor is rethrown as <see cref="DatabaseCircuitOpenException"/>.
/// Other failures, such as timeouts or transient errors that outlast the retries, propagate unchanged as
/// <see cref="ResilienceExecutionException"/>.
/// </remarks>
public sealed class CoreResilienceDatabaseExecutor : IDatabaseResilienceExecutor
{
    private readonly IResilienceExecutor _resilienceExecutor;
    private readonly ResilienceOptions _resilienceOptions;

    /// <summary>
    /// Creates the executor.
    /// </summary>
    /// <param name="resilienceExecutor">The QLS.Chassis.Core executor that applies timeouts, retries and the circuit breaker.</param>
    /// <param name="resilienceOptions">The database resilience policy.</param>
    public CoreResilienceDatabaseExecutor(
        IResilienceExecutor resilienceExecutor,
        ResilienceOptions resilienceOptions)
    {
        _resilienceExecutor = resilienceExecutor;
        _resilienceOptions = resilienceOptions;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            return await _resilienceExecutor.ExecuteAsync(_resilienceOptions, operation, cancellationToken);
        }
        catch (ResilienceExecutionException ex) when (ex.FailureType == ResilienceFailureType.CircuitOpen)
        {
            var retryAfter = DateTimeOffset.UtcNow.AddSeconds(ex.RetryAfterSeconds ?? _resilienceOptions.RetryAfterSeconds);
            throw new DatabaseCircuitOpenException(retryAfter);
        }
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await ExecuteAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }
}
