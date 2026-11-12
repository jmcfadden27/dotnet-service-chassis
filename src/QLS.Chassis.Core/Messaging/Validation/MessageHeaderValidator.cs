using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Messaging.Validation;

/// <summary>
/// Default <see cref="IMessageHeaderValidator"/>.
/// </summary>
public sealed class MessageHeaderValidator : IMessageHeaderValidator
{
    /// <inheritdoc />
    public void ValidateForPublish(MessageHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        if (header.SchemaVersion <= 0)
            throw new MessageHeaderValidationException("SchemaVersion must be greater than zero.");

        if (header.OccurredUtc == default)
            throw new MessageHeaderValidationException("OccurredUtc is required.");

        if (header.OccurredUtc.Kind != DateTimeKind.Utc)
            throw new MessageHeaderValidationException("OccurredUtc must be UTC.");

        if (string.IsNullOrWhiteSpace(header.CorrelationId))
            throw new MessageHeaderValidationException("CorrelationId is required.");

        if (string.IsNullOrWhiteSpace(header.MessageId))
            throw new MessageHeaderValidationException("MessageId is required.");

        if (string.IsNullOrWhiteSpace(header.MessageType))
            throw new MessageHeaderValidationException("MessageType is required.");

        if (string.IsNullOrWhiteSpace(header.Producer))
            throw new MessageHeaderValidationException("Producer is required.");

        if (string.IsNullOrWhiteSpace(header.TraceId))
            throw new MessageHeaderValidationException("TraceId is required.");
    }

    /// <inheritdoc />
    public void ValidateForDerivedPublish(MessageHeader header)
    {
        ValidateForPublish(header);

        if (string.IsNullOrWhiteSpace(header.CausationId))
            throw new MessageHeaderValidationException("CausationId is required for derived messages.");
    }
}
