namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Decides which HTTP requests need an idempotency key and how their results are kept.
/// </summary>
public interface IHttpIdempotencyPolicy
{
    /// <summary>
    /// The request header that carries the idempotency key, matched case-insensitively.
    /// </summary>
    string HeaderName { get; }
    /// <summary>
    /// How long an idempotency record lives.
    /// </summary>
    TimeSpan RecordTtl { get; }
    /// <summary>
    /// Whether requests with the given method need an idempotency key.
    /// </summary>
    /// <param name="httpMethod">The request's HTTP method.</param>
    /// <returns><see langword="true"/> when the method needs a key.</returns>
    bool RequiresForMethod(string? httpMethod);
    /// <summary>
    /// Whether a response should be stored for repeats; when not, the key is released.
    /// </summary>
    /// <param name="statusCode">The response status code.</param>
    /// <returns><see langword="true"/> to store the response.</returns>
    bool ShouldPersistResponse(int statusCode);
}
