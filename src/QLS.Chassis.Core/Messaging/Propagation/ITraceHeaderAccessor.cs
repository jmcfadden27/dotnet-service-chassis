namespace QLS.Chassis.Core.Messaging.Propagation;

/// <summary>
/// Supplies the trace headers of the current inbound request.
/// </summary>
public interface ITraceHeaderAccessor
{
    /// <summary>
    /// The inbound <c>X-Amzn-Trace-Id</c> header, or <see langword="null"/>.
    /// </summary>
    string? XAmznTraceId { get; }
    /// <summary>
    /// The inbound <c>traceparent</c> header, or <see langword="null"/>.
    /// </summary>
    string? TraceParent { get; }
}
