using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Messaging.Factory;

/// <summary>
/// Default <see cref="IMessageEnvelopeFactory"/>. Message IDs are new GUIDs without hyphens.
/// </summary>
public sealed class MessageEnvelopeFactory : IMessageEnvelopeFactory
{
    /// <inheritdoc />
    public MessageEnvelope<T> CreateRoot<T>(T body, string messageType, string producer, string correlationId, string traceId)
    {
        return new MessageEnvelope<T>
        {
            Header = BuildHeader(messageType, producer, correlationId, traceId, causationId: null),
            Body = body,
        };
    }

    /// <inheritdoc />
    public MessageEnvelope<T> CreateDerived<T>(T body, string messageType, string producer, string correlationId, string traceId, string causationId)
    {
        return new MessageEnvelope<T>
        {
            Header = BuildHeader(messageType, producer, correlationId, traceId, causationId),
            Body = body,
        };
    }

    private static MessageHeader BuildHeader(string messageType, string producer, string correlationId, string traceId, string? causationId)
    {
        return new MessageHeader
        {
            SchemaVersion = 1,
            OccurredUtc = DateTime.UtcNow,
            CorrelationId = correlationId,
            MessageId = Guid.NewGuid().ToString("N"),
            MessageType = messageType,
            Producer = producer,
            CausationId = causationId,
            TraceId = traceId,
        };
    }
}
