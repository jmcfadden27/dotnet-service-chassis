namespace QLS.Chassis.Core.Resilience;

/// <summary>
/// Supplies the current correlation ID for resilience log events and outbound header propagation.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// The current correlation ID, or <see langword="null"/> when there isn't one.
    /// </summary>
    string? CorrelationId { get; }
}
