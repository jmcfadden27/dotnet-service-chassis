using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Messaging.Factory;

/// <summary>
/// Creates message envelopes and validates their headers, so an invalid message is never published.
/// </summary>
public interface IValidatedMessageEnvelopeFactory
{
    /// <summary>
    /// Creates and validates a message that starts a chain.
    /// </summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="body">The message payload.</param>
    /// <param name="messageType">What happened, for example <c>order.created</c>.</param>
    /// <param name="producer">The sending service.</param>
    /// <param name="correlationId">The correlation ID of the request that started the chain.</param>
    /// <param name="traceId">The distributed trace ID.</param>
    /// <returns>The envelope.</returns>
    /// <exception cref="QLS.Chassis.Core.Messaging.Validation.MessageHeaderValidationException">The header fails validation.</exception>
    MessageEnvelope<T> CreateRoot<T>(T body, string messageType, string producer, string correlationId, string traceId);
    /// <summary>
    /// Creates and validates a message sent while handling another; a causation ID is required.
    /// </summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="body">The message payload.</param>
    /// <param name="messageType">What happened, for example <c>order.created</c>.</param>
    /// <param name="producer">The sending service.</param>
    /// <param name="correlationId">The incoming message's correlation ID.</param>
    /// <param name="traceId">The distributed trace ID.</param>
    /// <param name="causationId">The incoming message's <see cref="MessageHeader.MessageId"/>.</param>
    /// <returns>The envelope.</returns>
    /// <exception cref="QLS.Chassis.Core.Messaging.Validation.MessageHeaderValidationException">The header fails validation.</exception>
    MessageEnvelope<T> CreateDerived<T>(T body, string messageType, string producer, string correlationId, string traceId, string causationId);
}
