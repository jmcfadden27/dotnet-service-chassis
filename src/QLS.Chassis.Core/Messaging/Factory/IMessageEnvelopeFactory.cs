using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Messaging.Factory;

/// <summary>
/// Creates message envelopes with a filled-in header: schema version 1, the current UTC time and a new message ID.
/// </summary>
public interface IMessageEnvelopeFactory
{
    /// <summary>
    /// Creates a message that starts a chain, with no causation ID. The header is not validated.
    /// </summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="body">The message payload.</param>
    /// <param name="messageType">What happened, for example <c>order.created</c>.</param>
    /// <param name="producer">The sending service.</param>
    /// <param name="correlationId">The correlation ID of the request that started the chain.</param>
    /// <param name="traceId">The distributed trace ID.</param>
    /// <returns>The envelope.</returns>
    MessageEnvelope<T> CreateRoot<T>(T body, string messageType, string producer, string correlationId, string traceId);
    /// <summary>
    /// Creates a message sent while handling another. The header is not validated.
    /// </summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="body">The message payload.</param>
    /// <param name="messageType">What happened, for example <c>order.created</c>.</param>
    /// <param name="producer">The sending service.</param>
    /// <param name="correlationId">The incoming message's correlation ID.</param>
    /// <param name="traceId">The distributed trace ID.</param>
    /// <param name="causationId">The incoming message's <see cref="MessageHeader.MessageId"/>.</param>
    /// <returns>The envelope.</returns>
    MessageEnvelope<T> CreateDerived<T>(T body, string messageType, string producer, string correlationId, string traceId, string causationId);
}
