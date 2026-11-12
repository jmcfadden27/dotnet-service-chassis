namespace QLS.Chassis.WebApi.Data;

/// <summary>
/// Thrown by <see cref="IDatabaseResilienceExecutor"/> when the database circuit breaker is open and calls are being
/// rejected without reaching the database.
/// </summary>
/// <remarks>
/// <see cref="Exceptions.GlobalExceptionHandler"/> returns it as a 503 Service unavailable problem details response.
/// </remarks>
public sealed class DatabaseCircuitOpenException : Exception
{
    /// <summary>
    /// The UTC time after which the caller can try again.
    /// </summary>
    public DateTimeOffset RetryAfterUtc { get; }

    /// <summary>
    /// Creates the exception with the message "Database circuit breaker is open. Try again later."
    /// </summary>
    /// <param name="retryAfterUtc">The UTC time after which the caller can try again.</param>
    public DatabaseCircuitOpenException(DateTimeOffset retryAfterUtc)
        : base("Database circuit breaker is open. Try again later.")
    {
        RetryAfterUtc = retryAfterUtc;
    }
}
