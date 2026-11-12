namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Decides how message idempotency results are kept.
/// </summary>
public interface IMessageIdempotencyPolicy
{
    /// <summary>
    /// How long an idempotency record lives.
    /// </summary>
    TimeSpan RecordTtl { get; }
    /// <summary>
    /// Whether a response should be stored for repeats; when not, the key is released.
    /// </summary>
    /// <param name="statusCode">The response status code.</param>
    /// <returns><see langword="true"/> to store the response.</returns>
    bool ShouldPersistResponse(int statusCode);
}
