namespace QLS.Chassis.Core.Resilience;

/// <summary>
/// Controls timeout, retry, and circuit-breaker behavior for a single external dependency.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>
    /// Logical dependency identifier used to isolate circuit state and in resilience logs.
    /// Calls with the same name share one circuit.
    /// </summary>
    public required string DependencyName { get; init; }

    /// <summary>
    /// Per-attempt execution timeout in milliseconds.
    /// If a call exceeds this, the attempt is treated as a timeout failure and may be retried.
    /// </summary>
    public int TimeoutMs { get; init; } = 2500;

    /// <summary>
    /// Maximum number of retries after the initial attempt for transient failures.
    /// Total attempts = 1 + MaxRetries.
    /// </summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff between retries.
    /// Actual delay adds jitter to reduce synchronized retry spikes.
    /// </summary>
    public int BaseDelayMs { get; init; } = 200;

    /// <summary>
    /// Failure ratio threshold (0-1) that can open the circuit once minimum throughput is met.
    /// Example: 0.5 means open when at least 50% of sampled calls fail.
    /// </summary>
    public double CircuitFailureRatio { get; init; } = 0.5;

    /// <summary>
    /// Minimum number of sampled calls required before evaluating failure ratio.
    /// Prevents opening the circuit from very small sample sizes.
    /// </summary>
    public int CircuitMinThroughput { get; init; } = 10;

    /// <summary>
    /// How long the circuit stays open before allowing a half-open probe attempt.
    /// While open, calls fail fast without invoking the dependency.
    /// </summary>
    public int CircuitBreakSeconds { get; init; } = 30;

    /// <summary>
    /// Suggested retry delay for clients when fail-fast/circuit-open responses are returned.
    /// Carried on <see cref="ResilienceExecutionException.RetryAfterSeconds"/>, for callers to pass on to clients
    /// (for example as a Retry-After header).
    /// </summary>
    public int RetryAfterSeconds { get; init; } = 30;

    /// <summary>
    /// Optional classifier for dependency-specific transient failures.
    /// When provided, it augments the built-in transient detection.
    /// </summary>
    public Func<Exception, bool>? TransientExceptionClassifier { get; init; }
}
