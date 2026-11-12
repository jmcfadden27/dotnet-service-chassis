using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Messaging.Validation;

/// <summary>
/// Checks that a message header has the fields needed to trace and de-duplicate the message.
/// </summary>
public interface IMessageHeaderValidator
{
    /// <summary>
    /// Checks a header before it is sent or after it is received: <see cref="MessageHeader.SchemaVersion"/> above 0,
    /// <see cref="MessageHeader.OccurredUtc"/> set and in UTC, and correlation ID, message ID, message type, producer and
    /// trace ID present.
    /// </summary>
    /// <param name="header">The header to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="header"/> is null.</exception>
    /// <exception cref="MessageHeaderValidationException">A field is missing or invalid.</exception>
    void ValidateForPublish(MessageHeader header);
    /// <summary>
    /// Runs <see cref="ValidateForPublish"/> and also requires a causation ID.
    /// </summary>
    /// <param name="header">The header to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="header"/> is null.</exception>
    /// <exception cref="MessageHeaderValidationException">A field is missing or invalid.</exception>
    void ValidateForDerivedPublish(MessageHeader header);
}
