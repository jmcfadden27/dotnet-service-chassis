namespace QLS.Chassis.Core.Messaging.Headers;

/// <summary>
/// Names of the HTTP headers the chassis reads and writes.
/// </summary>
public static class ChassisHeaderNames
{
    /// <summary>
    /// Carries the correlation ID.
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";
    /// <summary>
    /// Carries the causation ID.
    /// </summary>
    public const string CausationId = "X-Causation-Id";
    /// <summary>
    /// The AWS X-Ray trace header.
    /// </summary>
    public const string XAmznTraceId = "X-Amzn-Trace-Id";
    /// <summary>
    /// The W3C trace context header.
    /// </summary>
    public const string TraceParent = "traceparent";
}
