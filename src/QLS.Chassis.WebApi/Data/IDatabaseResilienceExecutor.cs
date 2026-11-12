namespace QLS.Chassis.WebApi.Data;

/// <summary>
/// Runs database operations with the registered resilience policy: a per-attempt timeout, retries for transient
/// failures and a circuit breaker.
/// </summary>
/// <remarks>
/// Open the connection inside the operation so each retry gets a fresh one, and pass the attempt's token to the
/// query so the per-attempt timeout can cancel it. Retried operations should be safe to repeat.
/// </remarks>
public interface IDatabaseResilienceExecutor
{
    /// <summary>
    /// Runs an operation that returns a value through the resilience policy.
    /// </summary>
    /// <typeparam name="T">The type of the operation's result.</typeparam>
    /// <param name="operation">
    /// The operation to run. It is called once per attempt with a token that is cancelled when the attempt times out
    /// or <paramref name="cancellationToken"/> is cancelled.
    /// </param>
    /// <param name="cancellationToken">A token that cancels the whole call, including any retries.</param>
    /// <returns>The result of the first successful attempt.</returns>
    /// <exception cref="DatabaseCircuitOpenException">The circuit breaker for the database is open.</exception>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
    /// <summary>
    /// Runs an operation that returns no value through the resilience policy.
    /// </summary>
    /// <param name="operation">
    /// The operation to run. It is called once per attempt with a token that is cancelled when the attempt times out
    /// or <paramref name="cancellationToken"/> is cancelled.
    /// </param>
    /// <param name="cancellationToken">A token that cancels the whole call, including any retries.</param>
    /// <returns>A task that completes when an attempt succeeds.</returns>
    /// <exception cref="DatabaseCircuitOpenException">The circuit breaker for the database is open.</exception>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
