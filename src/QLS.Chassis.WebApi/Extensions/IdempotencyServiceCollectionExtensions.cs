using Microsoft.Extensions.DependencyInjection;
using QLS.Chassis.Core.Idempotency;
using QLS.Chassis.WebApi.Idempotency;

namespace QLS.Chassis.WebApi.Extensions;

/// <summary>
/// Extension methods that register the chassis HTTP idempotency services.
/// </summary>
public static class IdempotencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers HTTP idempotency with records in the default <c>public.idempotency_records</c> table
    /// (<see cref="HttpIdempotencyStorePolicies.Default"/>).
    /// </summary>
    /// <remarks>
    /// Requires <see cref="DatabaseServiceCollectionExtensions.AddChassisPostgresDatabaseResiliency"/> and
    /// <see cref="ServiceCollectionExtensions.AddChassisWebApi"/>. See the overload that takes a
    /// <see cref="HttpIdempotencyStorePolicy"/> for what is registered.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="policy">The idempotency policy, such as <see cref="HttpIdempotencyPolicies.Default"/>.</param>
    /// <param name="cleanupPolicy">The cleanup policy, such as <see cref="HttpIdempotencyCleanupPolicies.Default"/>.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> or <paramref name="cleanupPolicy"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddChassisHttpIdempotency(
        this IServiceCollection services,
        IHttpIdempotencyPolicy policy,
        HttpIdempotencyCleanupPolicy cleanupPolicy)
    {
        return services.AddChassisHttpIdempotency(policy, cleanupPolicy, HttpIdempotencyStorePolicies.Default);
    }

    /// <summary>
    /// Registers HTTP idempotency with records in the table named by <paramref name="storePolicy"/>: the policies,
    /// <see cref="PostgresHttpIdempotencyStore"/> as <see cref="IIdempotencyStore"/>, QLS.Chassis.Core's
    /// <see cref="HttpIdempotencyCoordinator"/> and <see cref="HttpIdempotencyCleanupHostedService"/>.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="DatabaseServiceCollectionExtensions.AddChassisPostgresDatabaseResiliency"/>, because records
    /// are stored in Postgres, and <see cref="ServiceCollectionExtensions.AddChassisWebApi"/>, because the cleanup service
    /// logs through <see cref="Observability.IStructuredAppLogger"/>. Endpoints opt in with
    /// <see cref="HttpIdempotentAttribute"/>, and the table must already exist.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="policy">The idempotency policy, such as <see cref="HttpIdempotencyPolicies.Default"/>.</param>
    /// <param name="cleanupPolicy">The cleanup policy, such as <see cref="HttpIdempotencyCleanupPolicies.Default"/>.</param>
    /// <param name="storePolicy">The schema and table that hold the records.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="policy"/>, <paramref name="cleanupPolicy"/> or <paramref name="storePolicy"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddChassisHttpIdempotency(
        this IServiceCollection services,
        IHttpIdempotencyPolicy policy,
        HttpIdempotencyCleanupPolicy cleanupPolicy,
        HttpIdempotencyStorePolicy storePolicy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(cleanupPolicy);
        ArgumentNullException.ThrowIfNull(storePolicy);

        services.AddSingleton(policy);
        services.AddSingleton(cleanupPolicy);
        services.AddSingleton(storePolicy);
        services.AddSingleton<IIdempotencyStore, PostgresHttpIdempotencyStore>();
        services.AddSingleton<IHttpIdempotencyCoordinator, HttpIdempotencyCoordinator>();
        services.AddHostedService<HttpIdempotencyCleanupHostedService>();

        return services;
    }
}
