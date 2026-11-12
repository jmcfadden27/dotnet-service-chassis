namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Runs an HTTP request's operation at most once per idempotency key, returning the stored response to repeats.
/// </summary>
public interface IHttpIdempotencyCoordinator
{
    /// <summary>
    /// Runs <paramref name="operation"/> once for the request's idempotency key. When the policy doesn't cover the request's
    /// method, the operation just runs. A response the policy keeps is stored; any other response, or an exception, releases
    /// the key so the client can retry.
    /// </summary>
    /// <param name="request">The request, including the headers that carry the idempotency key.</param>
    /// <param name="operation">The work to run when this request claims the key.</param>
    /// <returns>The operation's response, or the stored response for a repeat of a completed request.</returns>
    /// <exception cref="IdempotencyKeyRequiredException">The key header is missing, blank or longer than 128 characters.</exception>
    /// <exception cref="IdempotencyKeyReuseConflictException">The key was already used for a different request.</exception>
    /// <exception cref="IdempotencyRequestInProgressException">A request with the same key is still running.</exception>
    Task<HttpIdempotencyExecutionResponse> ExecuteAsync(
        HttpIdempotencyExecutionRequest request,
        Func<Task<HttpIdempotencyExecutionResponse>> operation);
}
