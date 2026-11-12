namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Thrown when an idempotency key is required but missing or blank, or is longer than 128 characters.
/// </summary>
public sealed class IdempotencyKeyRequiredException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    public IdempotencyKeyRequiredException(string message) : base(message) { }
}

/// <summary>
/// Thrown when an idempotency key is reused for a request or message with a different fingerprint.
/// </summary>
public sealed class IdempotencyKeyReuseConflictException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    public IdempotencyKeyReuseConflictException(string message) : base(message) { }
}

/// <summary>
/// Thrown when another request or message with the same idempotency key is still being processed.
/// </summary>
public sealed class IdempotencyRequestInProgressException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    public IdempotencyRequestInProgressException(string message) : base(message) { }
}
