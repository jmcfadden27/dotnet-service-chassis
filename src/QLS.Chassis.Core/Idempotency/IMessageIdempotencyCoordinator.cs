namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Runs a message's operation at most once per idempotency key, returning the stored response to repeats.
/// </summary>
public interface IMessageIdempotencyCoordinator
{
    /// <summary>
    /// Runs <paramref name="operation"/> once for the message's idempotency key. A response the policy keeps is stored; any
    /// other response, or an exception, releases the key so the message can be retried.
    /// </summary>
    /// <param name="request">The message's consumer, operation, idempotency key and body.</param>
    /// <param name="operation">The work to run when this message claims the key.</param>
    /// <returns>The operation's response, or the stored response for a repeat of a completed message.</returns>
    /// <exception cref="IdempotencyKeyRequiredException">The idempotency key is blank or longer than 128 characters.</exception>
    /// <exception cref="IdempotencyKeyReuseConflictException">The key was already used for a different message.</exception>
    /// <exception cref="IdempotencyRequestInProgressException">A message with the same key is still being processed.</exception>
    Task<MessageIdempotencyExecutionResponse> ExecuteAsync(
        MessageIdempotencyExecutionRequest request,
        Func<Task<MessageIdempotencyExecutionResponse>> operation);
}
