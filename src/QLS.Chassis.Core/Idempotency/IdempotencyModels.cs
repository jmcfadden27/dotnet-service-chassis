namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// An HTTP request as seen by <see cref="IHttpIdempotencyCoordinator"/>.
/// </summary>
public sealed class HttpIdempotencyExecutionRequest
{
    /// <summary>
    /// The logical operation, part of the record key and the fingerprint.
    /// </summary>
    public string OperationName { get; init; } = string.Empty;
    /// <summary>
    /// The HTTP method, checked against <see cref="IHttpIdempotencyPolicy.RequiresForMethod"/>.
    /// </summary>
    public string HttpMethod { get; init; } = string.Empty;
    /// <summary>
    /// The request path, part of the fingerprint.
    /// </summary>
    public string Path { get; init; } = string.Empty;
    /// <summary>
    /// The request body, part of the fingerprint.
    /// </summary>
    public string Body { get; init; } = string.Empty;
    /// <summary>
    /// The request headers. The idempotency key header is found case-insensitively.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The query string parameters, sorted by name into the fingerprint.
    /// </summary>
    public IReadOnlyDictionary<string, string> QueryStringParameters { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The caller's user ID, which scopes the record key. Blank values use <c>anonymous</c>.
    /// </summary>
    public string UserId { get; init; } = string.Empty;
}

/// <summary>
/// An HTTP response produced by an idempotent operation, and stored for repeats.
/// </summary>
public sealed class HttpIdempotencyExecutionResponse
{
    /// <summary>
    /// The response status code.
    /// </summary>
    public int StatusCode { get; init; }
    /// <summary>
    /// The response body.
    /// </summary>
    public string? Body { get; init; }
    /// <summary>
    /// The response headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// An idempotency record as returned by <see cref="IIdempotencyStore.LoadAsync"/>.
/// </summary>
public sealed class IdempotencyStoreRecord
{
    /// <summary>
    /// The record's status. <c>COMPLETED</c> once a response has been saved.
    /// </summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>
    /// The fingerprint of the request that claimed the key.
    /// </summary>
    public string Fingerprint { get; init; } = string.Empty;
    /// <summary>
    /// The stored response as JSON, empty until the record is completed.
    /// </summary>
    public string ResponseJson { get; init; } = string.Empty;
}
