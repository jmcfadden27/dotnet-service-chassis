namespace QLS.Chassis.Core.Messaging.Propagation;

/// <summary>
/// An <see cref="ITraceHeaderAccessor"/> with no inbound headers.
/// </summary>
public sealed class NullTraceHeaderAccessor : ITraceHeaderAccessor
{
    /// <inheritdoc />
    public string? XAmznTraceId => null;
    /// <inheritdoc />
    public string? TraceParent => null;
}
