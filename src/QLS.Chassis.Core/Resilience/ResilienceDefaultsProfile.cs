namespace QLS.Chassis.Core.Resilience;

/// <summary>
/// The default resilience settings used by <see cref="ResilienceDefaults"/>. Subclass it and override properties to change them.
/// </summary>
public class ResilienceDefaultsProfile
{
    /// <summary>
    /// Per-attempt timeout in milliseconds. Defaults to 2500.
    /// </summary>
    public virtual int TimeoutMs => 2500;
    /// <summary>
    /// Retries after the first attempt. Defaults to 2.
    /// </summary>
    public virtual int MaxRetries => 2;
    /// <summary>
    /// Base delay in milliseconds for exponential backoff. Defaults to 200.
    /// </summary>
    public virtual int BaseDelayMs => 200;
    /// <summary>
    /// Failure ratio that opens the circuit. Defaults to 0.5.
    /// </summary>
    public virtual double CircuitFailureRatio => 0.5;
    /// <summary>
    /// Calls sampled before the failure ratio is evaluated. Defaults to 10.
    /// </summary>
    public virtual int CircuitMinThroughput => 10;
    /// <summary>
    /// How long the circuit stays open, in seconds. Defaults to 30.
    /// </summary>
    public virtual int CircuitBreakSeconds => 30;
    /// <summary>
    /// Suggested wait for clients after a failure, in seconds. Defaults to 30.
    /// </summary>
    public virtual int RetryAfterSeconds => 30;
}
