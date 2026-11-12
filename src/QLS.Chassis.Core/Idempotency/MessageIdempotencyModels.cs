namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// A message as seen by <see cref="IMessageIdempotencyCoordinator"/>.
/// </summary>
public sealed class MessageIdempotencyExecutionRequest
{
    /// <summary>
    /// The logical operation, part of the record key and the fingerprint.
    /// </summary>
    public string OperationName { get; init; } = string.Empty;
    /// <summary>
    /// The consuming service, which scopes the record key. Blank values use <c>consumer</c>.
    /// </summary>
    public string ConsumerName { get; init; } = string.Empty;
    /// <summary>
    /// The message's idempotency key, usually its message ID. Required, at most 128 characters after trimming.
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;
    /// <summary>
    /// The message body, part of the fingerprint.
    /// </summary>
    public string MessageBody { get; init; } = string.Empty;
}

/// <summary>
/// The result of an idempotent message operation, and stored for repeats.
/// </summary>
public sealed class MessageIdempotencyExecutionResponse
{
    /// <summary>
    /// A status code for the result, checked against <see cref="IMessageIdempotencyPolicy.ShouldPersistResponse"/>.
    /// </summary>
    public int StatusCode { get; init; }
    /// <summary>
    /// The result body.
    /// </summary>
    public string? Body { get; init; }
}
