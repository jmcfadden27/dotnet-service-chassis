using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.WebApi.Data;

/// <summary>
/// Readiness health check that opens a connection to PostgreSQL and runs <c>SELECT 1</c>.
/// </summary>
/// <remarks>
/// Registered as <c>postgres</c> with the <c>ready</c> tag by
/// <see cref="Extensions.DatabaseServiceCollectionExtensions.AddChassisPostgresDatabaseResiliency"/>, so it is part
/// of <c>/health/ready</c>.
/// </remarks>
public sealed class PostgresReadinessHealthCheck : IHealthCheck
{
    private readonly ChassisDatabaseOptions _databaseOptions;
    private readonly ResilienceOptions _policy;

    /// <summary>
    /// Creates the health check.
    /// </summary>
    /// <param name="databaseOptions">The connection string to check.</param>
    /// <param name="policy">The database resilience policy; its <see cref="ResilienceOptions.TimeoutMs"/> sets the command timeout.</param>
    public PostgresReadinessHealthCheck(ChassisDatabaseOptions databaseOptions, ResilienceOptions policy)
    {
        _databaseOptions = databaseOptions;
        _policy = policy;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The command timeout is the policy's timeout rounded up to whole seconds, at least 1. The check does not go through
    /// the resilience executor and never throws: any exception gives an unhealthy result that carries it.
    /// </remarks>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_databaseOptions.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = Math.Max(1, (int)Math.Ceiling(_policy.TimeoutMs / 1000d));
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL readiness check failed.", ex);
        }
    }
}
