namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Default <see cref="IMessageIdempotencyPolicy"/>: keeps records for 24 hours and stores responses with a status code below 500.
/// </summary>
public sealed class DefaultMessageIdempotencyPolicy : IMessageIdempotencyPolicy
{
    /// <inheritdoc />
    public TimeSpan RecordTtl => TimeSpan.FromHours(24);

    /// <inheritdoc />
    public bool ShouldPersistResponse(int statusCode)
    {
        return statusCode < 500;
    }
}
