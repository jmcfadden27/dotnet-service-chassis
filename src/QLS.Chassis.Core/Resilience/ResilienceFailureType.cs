namespace QLS.Chassis.Core.Resilience;

/// <summary>
/// Why a resilient call failed.
/// </summary>
public enum ResilienceFailureType
{
    /// <summary>
    /// The last attempt exceeded <see cref="ResilienceOptions.TimeoutMs"/>.
    /// </summary>
    Timeout,
    /// <summary>
    /// The circuit was open, so the call failed fast without reaching the dependency.
    /// </summary>
    CircuitOpen,
    /// <summary>
    /// Transient failures used up every retry.
    /// </summary>
    DependencyUnavailable,
}
