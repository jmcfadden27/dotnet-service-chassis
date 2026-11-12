using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// A parsed message envelope and the values taken from its header.
/// </summary>
/// <typeparam name="TMessage">The envelope body type.</typeparam>
public sealed class MessageEnvelopeProcessingResult<TMessage>
{
    /// <summary>
    /// The parsed envelope.
    /// </summary>
    public required MessageEnvelope<TMessage> Envelope { get; init; }
    /// <summary>
    /// The header values and transport ID.
    /// </summary>
    public required MessageProcessingContext Context { get; init; }
}
