namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Turns a raw message body into a validated <see cref="QLS.Chassis.Core.Messaging.Contracts.MessageEnvelope{T}"/>.
/// </summary>
public interface IMessageEnvelopeProcessor
{
    /// <summary>
    /// Deserializes the body, checks that the envelope, header and body are present, and validates the header.
    /// </summary>
    /// <typeparam name="TMessage">The envelope body type.</typeparam>
    /// <param name="rawMessageBody">The raw JSON message.</param>
    /// <param name="transportMessageId">The transport's message ID, copied to the context.</param>
    /// <param name="cancellationToken">A cancellation token. <see cref="MessageEnvelopeProcessor"/> completes synchronously and does not observe it.</param>
    /// <returns>The envelope and its <see cref="MessageProcessingContext"/>.</returns>
    /// <exception cref="Newtonsoft.Json.JsonException">The body isn't valid JSON for the envelope.</exception>
    /// <exception cref="InvalidOperationException">The envelope, its header or its body is null.</exception>
    /// <exception cref="QLS.Chassis.Core.Messaging.Validation.MessageHeaderValidationException">The header fails validation.</exception>
    Task<MessageEnvelopeProcessingResult<TMessage>> ProcessAsync<TMessage>(
        string rawMessageBody,
        string? transportMessageId = null,
        CancellationToken cancellationToken = default);
}
