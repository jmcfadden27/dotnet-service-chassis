namespace QLS.Chassis.WebApi.Idempotency;

/// <summary>
/// Settings for <see cref="HttpIdempotencyCleanupHostedService"/>. <see cref="HttpIdempotencyCleanupPolicies.Default"/>
/// has the defaults.
/// </summary>
public sealed class HttpIdempotencyCleanupPolicy
{
    /// <summary>
    /// The time to wait between cleanup runs.
    /// </summary>
    public required TimeSpan CleanupInterval { get; init; }
    /// <summary>
    /// The most expired records deleted in one run.
    /// </summary>
    public required int CleanupBatchSize { get; init; }
}
