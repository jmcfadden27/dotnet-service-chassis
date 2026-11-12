namespace QLS.Chassis.Core.Messaging.Propagation;

/// <summary>
/// Adds correlation and trace headers to outgoing HTTP requests, so the next service continues the same trace.
/// </summary>
public interface IOutboundHeaderPropagator
{
    /// <summary>
    /// Sets the <c>X-Correlation-Id</c>, <c>X-Amzn-Trace-Id</c> and <c>traceparent</c> headers, replacing any already on the request.
    /// </summary>
    /// <param name="request">The outgoing request.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    /// <exception cref="OutboundHeaderPropagationException">There is no current correlation ID.</exception>
    void Apply(HttpRequestMessage request);
}
