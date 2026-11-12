namespace QLS.Chassis.Core.Logging;

/// <summary>
/// One JSON log line in the shape shared by every chassis service.
/// </summary>
public sealed class StructuredLogEvent
{
    /// <summary>
    /// The version of this event shape. Defaults to 1.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;
    /// <summary>
    /// When the event was created, as a UTC round-trip (<c>O</c>) timestamp.
    /// </summary>
    public string TimestampUtc { get; init; } = DateTime.UtcNow.ToString("O");
    /// <summary>
    /// The log level name. Defaults to <c>Information</c>.
    /// </summary>
    public string Level { get; init; } = "Information";
    /// <summary>
    /// What happened, usually one of <see cref="LogEventNames"/>.
    /// </summary>
    public string EventName { get; init; } = string.Empty;
    /// <summary>
    /// The service writing the event.
    /// </summary>
    public string Service { get; init; } = string.Empty;
    /// <summary>
    /// The operation the event is about, for example <c>POST /orders</c>.
    /// </summary>
    public string? Operation { get; init; }
    /// <summary>
    /// The ID shared by everything one original request caused.
    /// </summary>
    public string? CorrelationId { get; init; }
    /// <summary>
    /// The distributed trace ID.
    /// </summary>
    public string? TraceId { get; init; }
    /// <summary>
    /// The ID of the request or invocation.
    /// </summary>
    public string? RequestId { get; init; }
    /// <summary>
    /// The ID of the message being handled, if any.
    /// </summary>
    public string? MessageId { get; init; }
    /// <summary>
    /// The ID of the message that caused this one, if any.
    /// </summary>
    public string? CausationId { get; init; }
    /// <summary>
    /// The user the request was made for, if any.
    /// </summary>
    public string? UserId { get; init; }
    /// <summary>
    /// The response status code, if any.
    /// </summary>
    public int? StatusCode { get; init; }
    /// <summary>
    /// How long the operation took, in milliseconds.
    /// </summary>
    public long? DurationMs { get; init; }
    /// <summary>
    /// A broad error category such as <c>validation</c> or <c>dependency_failure</c>, for failure events.
    /// </summary>
    public string? ErrorCategory { get; init; }
    /// <summary>
    /// The exception type name, for failure events.
    /// </summary>
    public string? ErrorType { get; init; }
    /// <summary>
    /// The exception message, for failure events.
    /// </summary>
    public string? ErrorMessage { get; init; }
    /// <summary>
    /// The exception text with stack trace, for failure events.
    /// </summary>
    public string? ErrorStackTrace { get; init; }
    /// <summary>
    /// Extra event-specific values.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}
