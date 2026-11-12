using QLS.Chassis.Core.Messaging.Contracts;
using QLS.Chassis.Core.Messaging.Validation;

namespace QLS.Chassis.Core.Messaging.Factory;

/// <summary>
/// Default <see cref="IValidatedMessageEnvelopeFactory"/>: an <see cref="IMessageEnvelopeFactory"/> followed by an <see cref="IMessageHeaderValidator"/>.
/// </summary>
public sealed class ValidatedMessageEnvelopeFactory : IValidatedMessageEnvelopeFactory
{
    private readonly IMessageEnvelopeFactory _innerFactory;
    private readonly IMessageHeaderValidator _validator;

    /// <summary>
    /// Creates the factory.
    /// </summary>
    /// <param name="innerFactory">Creates the envelopes.</param>
    /// <param name="validator">Validates each envelope's header.</param>
    public ValidatedMessageEnvelopeFactory(IMessageEnvelopeFactory innerFactory, IMessageHeaderValidator validator)
    {
        _innerFactory = innerFactory;
        _validator = validator;
    }

    /// <inheritdoc />
    public MessageEnvelope<T> CreateRoot<T>(T body, string messageType, string producer, string correlationId, string traceId)
    {
        var envelope = _innerFactory.CreateRoot(body, messageType, producer, correlationId, traceId);
        _validator.ValidateForPublish(envelope.Header);
        return envelope;
    }

    /// <inheritdoc />
    public MessageEnvelope<T> CreateDerived<T>(T body, string messageType, string producer, string correlationId, string traceId, string causationId)
    {
        var envelope = _innerFactory.CreateDerived(body, messageType, producer, correlationId, traceId, causationId);
        _validator.ValidateForDerivedPublish(envelope.Header);
        return envelope;
    }
}
