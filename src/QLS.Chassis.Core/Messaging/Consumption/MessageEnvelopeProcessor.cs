using Newtonsoft.Json;
using QLS.Chassis.Core.Messaging.Contracts;
using QLS.Chassis.Core.Messaging.Validation;

namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Default <see cref="IMessageEnvelopeProcessor"/>, using Newtonsoft.Json and <see cref="IMessageHeaderValidator.ValidateForPublish"/>.
/// </summary>
public sealed class MessageEnvelopeProcessor : IMessageEnvelopeProcessor
{
    private readonly IMessageHeaderValidator _headerValidator;

    /// <summary>
    /// Creates the processor.
    /// </summary>
    /// <param name="headerValidator">Validates each envelope's header.</param>
    public MessageEnvelopeProcessor(IMessageHeaderValidator headerValidator)
    {
        _headerValidator = headerValidator;
    }

    /// <inheritdoc />
    public Task<MessageEnvelopeProcessingResult<TMessage>> ProcessAsync<TMessage>(
        string rawMessageBody,
        string? transportMessageId = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = JsonConvert.DeserializeObject<MessageEnvelope<TMessage>>(rawMessageBody);
        if (envelope == null)
            throw new InvalidOperationException($"Deserialized MessageEnvelope<{typeof(TMessage).Name}> is null");

        if (envelope.Header == null)
            throw new InvalidOperationException("Envelope.Header is null");

        if (envelope.Body == null)
            throw new InvalidOperationException($"Envelope.Body ({typeof(TMessage).Name}) is null");

        _headerValidator.ValidateForPublish(envelope.Header);

        var context = new MessageProcessingContext
        {
            CorrelationId = envelope.Header.CorrelationId ?? string.Empty,
            TraceId = envelope.Header.TraceId ?? string.Empty,
            MessageId = envelope.Header.MessageId ?? string.Empty,
            CausationId = envelope.Header.CausationId,
            MessageType = envelope.Header.MessageType ?? string.Empty,
            Producer = envelope.Header.Producer ?? string.Empty,
            SchemaVersion = envelope.Header.SchemaVersion,
            OccurredUtc = envelope.Header.OccurredUtc,
            TransportMessageId = transportMessageId,
        };

        return Task.FromResult(new MessageEnvelopeProcessingResult<TMessage>
        {
            Envelope = envelope,
            Context = context,
        });
    }
}
