namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// The header values of a received message, plus its transport ID. Null header strings become empty strings.
/// </summary>
public sealed class MessageProcessingContext
{
    /// <summary>
    /// The message's correlation ID.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;
    /// <summary>
    /// The message's trace ID.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;
    /// <summary>
    /// The message's ID.
    /// </summary>
    public string MessageId { get; init; } = string.Empty;
    /// <summary>
    /// The ID of the message that caused this one, or <see langword="null"/> for a root message.
    /// </summary>
    public string? CausationId { get; init; }
    /// <summary>
    /// The message type.
    /// </summary>
    public string MessageType { get; init; } = string.Empty;
    /// <summary>
    /// The service that sent the message.
    /// </summary>
    public string Producer { get; init; } = string.Empty;
    /// <summary>
    /// The envelope schema version.
    /// </summary>
    public int SchemaVersion { get; init; }
    /// <summary>
    /// When the message was created, in UTC.
    /// </summary>
    public DateTime OccurredUtc { get; init; }
    /// <summary>
    /// The transport's own message ID, if one was given.
    /// </summary>
    public string? TransportMessageId { get; init; }
}
