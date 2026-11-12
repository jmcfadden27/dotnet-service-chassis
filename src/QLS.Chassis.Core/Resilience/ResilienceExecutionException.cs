namespace QLS.Chassis.Core.Resilience;

/// <summary>
/// Thrown by <see cref="IResilienceExecutor"/> when a call fails fast on an open circuit or gives up after its retries.
/// </summary>
public sealed class ResilienceExecutionException : Exception
{
    /// <summary>
    /// The dependency that failed.
    /// </summary>
    public string DependencyName { get; }
    /// <summary>
    /// Why the call failed.
    /// </summary>
    public ResilienceFailureType FailureType { get; }
    /// <summary>
    /// Suggested seconds to wait before trying again, for example for a <c>Retry-After</c> header. For an open circuit it is
    /// the time left until the circuit allows a probe; otherwise <see cref="ResilienceOptions.RetryAfterSeconds"/>.
    /// </summary>
    public int? RetryAfterSeconds { get; }

    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="dependencyName">The dependency that failed.</param>
    /// <param name="failureType">Why the call failed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The last underlying failure, if any.</param>
    /// <param name="retryAfterSeconds">Suggested seconds to wait before trying again, if any.</param>
    public ResilienceExecutionException(
        string dependencyName,
        ResilienceFailureType failureType,
        string message,
        Exception? innerException = null,
        int? retryAfterSeconds = null) : base(message, innerException)
    {
        DependencyName = dependencyName;
        FailureType = failureType;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
