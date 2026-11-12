namespace QLS.Chassis.Core.Resilience;


/// <summary>
/// Runs operations against a named dependency with a per-attempt timeout, retries for transient failures and a circuit breaker.
/// </summary>
public interface IResilienceExecutor
{
    /// <summary>
    /// Runs <paramref name="operation"/>, retrying timeouts and transient failures up to <see cref="ResilienceOptions.MaxRetries"/>
    /// times with exponential backoff and jitter. Any other exception is rethrown unchanged without a retry.
    /// </summary>
    /// <typeparam name="T">The operation's result type.</typeparam>
    /// <param name="options">The timeout, retry and circuit settings, including the dependency name that selects the circuit.</param>
    /// <param name="operation">The call to make. Its token is cancelled when the attempt times out or <paramref name="cancellationToken"/> is cancelled.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The operation's result.</returns>
    /// <exception cref="ResilienceExecutionException">The circuit is open, or every attempt timed out or failed transiently.</exception>
    Task<T> ExecuteAsync<T>(ResilienceOptions options, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
