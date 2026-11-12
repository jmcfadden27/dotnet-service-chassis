namespace QLS.Chassis.Core.Messaging.Propagation;

/// <summary>
/// Thrown when outbound headers can't be set, because there is no current correlation ID.
/// </summary>
public sealed class OutboundHeaderPropagationException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    public OutboundHeaderPropagationException(string message)
        : base(message)
    {
    }
}
