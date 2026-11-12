namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Persists idempotency records for the HTTP and message coordinators. Times are Unix epoch seconds.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically creates an in-progress record for the key, or replaces one that has expired.
    /// </summary>
    /// <param name="key">The record key.</param>
    /// <param name="fingerprint">A hash of the request, used to detect a key reused for a different request.</param>
    /// <param name="expiresAtEpochSeconds">When the new record expires.</param>
    /// <param name="nowEpochSeconds">The current time, for deciding whether an existing record has expired.</param>
    /// <returns><see langword="true"/> only if this call claimed the key.</returns>
    Task<bool> TryClaimAsync(string key, string fingerprint, long expiresAtEpochSeconds, long nowEpochSeconds);
    /// <summary>
    /// Loads the record for a key.
    /// </summary>
    /// <param name="key">The record key.</param>
    /// <returns>The record, or <see langword="null"/> when there isn't one.</returns>
    Task<IdempotencyStoreRecord?> LoadAsync(string key);
    /// <summary>
    /// Marks the record <c>COMPLETED</c> and stores the response as JSON.
    /// </summary>
    /// <param name="key">The record key.</param>
    /// <param name="fingerprint">The request's fingerprint.</param>
    /// <param name="response">The response to store.</param>
    /// <param name="expiresAtEpochSeconds">When the record expires.</param>
    /// <returns>A task that completes when the record is saved.</returns>
    Task SaveCompletedAsync(string key, string fingerprint, HttpIdempotencyExecutionResponse response, long expiresAtEpochSeconds);
    /// <summary>
    /// Removes the record for a key.
    /// </summary>
    /// <param name="key">The record key.</param>
    /// <returns>A task that completes when the record is removed.</returns>
    Task DeleteAsync(string key);
}
