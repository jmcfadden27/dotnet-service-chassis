using QLS.Chassis.Core.Idempotency;

namespace QLS.Chassis.WebApi.Idempotency;

/// <summary>
/// Ready-made idempotency policies for
/// <see cref="Extensions.IdempotencyServiceCollectionExtensions.AddChassisHttpIdempotency(Microsoft.Extensions.DependencyInjection.IServiceCollection, IHttpIdempotencyPolicy, HttpIdempotencyCleanupPolicy)"/>.
/// </summary>
public static class HttpIdempotencyPolicies
{
    /// <summary>
    /// QLS.Chassis.Core's <see cref="DefaultHttpIdempotencyPolicy"/>: the <c>Idempotency-Key</c> header, POST, PUT, PATCH and
    /// DELETE covered, records kept for 24 hours, and responses below 500 stored.
    /// </summary>
    public static readonly IHttpIdempotencyPolicy Default = new DefaultHttpIdempotencyPolicy();
}

/// <summary>
/// Ready-made cleanup policies for <see cref="HttpIdempotencyCleanupHostedService"/>.
/// </summary>
public static class HttpIdempotencyCleanupPolicies
{
    /// <summary>
    /// Cleans up every 15 minutes, deleting at most 500 expired records per run.
    /// </summary>
    public static readonly HttpIdempotencyCleanupPolicy Default = new()
    {
        CleanupInterval = TimeSpan.FromMinutes(15),
        CleanupBatchSize = 500,
    };
}
