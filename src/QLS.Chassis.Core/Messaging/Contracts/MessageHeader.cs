namespace QLS.Chassis.Core.Messaging.Contracts;

/// <summary>
/// The metadata carried with every message.
/// </summary>
public sealed class MessageHeader
{
    /// <summary>
    /// The envelope version. The factory sets 1; validation requires a value above 0.
    /// </summary>
    public int SchemaVersion { get; set; }
    /// <summary>
    /// When the message was created. Validation requires it to be set and of kind <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    public DateTime OccurredUtc { get; set; }
    /// <summary>
    /// The ID shared by everything one original request caused.
    /// </summary>
    public string? CorrelationId { get; set; }
    /// <summary>
    /// The message's unique ID. The factory sets a new GUID.
    /// </summary>
    public string? MessageId { get; set; }
    /// <summary>
    /// What happened, for example <c>order.created</c>.
    /// </summary>
    public string? MessageType { get; set; }
    /// <summary>
    /// The service that sent the message.
    /// </summary>
    public string? Producer { get; set; }
    /// <summary>
    /// The <see cref="MessageId"/> of the message that caused this one. Required for derived messages.
    /// </summary>
    public string? CausationId { get; set; }
    /// <summary>
    /// The distributed trace ID.
    /// </summary>
    public string? TraceId { get; set; }
    /// <summary>
    /// The distributed span ID. Not set by the factory or checked by validation.
    /// </summary>
    public string? SpanId { get; set; }
}
