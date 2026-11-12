using Dapper;
using Microsoft.Extensions.Hosting;
using Npgsql;
using QLS.Chassis.Core.Resilience;
using QLS.Chassis.WebApi.Data;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Idempotency;

/// <summary>
/// Background service that deletes expired HTTP idempotency records from Postgres.
/// </summary>
/// <remarks>
/// Runs once at start-up and then every <see cref="HttpIdempotencyCleanupPolicy.CleanupInterval"/>, deleting at most
/// <see cref="HttpIdempotencyCleanupPolicy.CleanupBatchSize"/> records whose expiry has passed on each run, through
/// <see cref="IDatabaseResilienceExecutor"/>. Runs that delete records are logged as
/// <c>http_idempotency_cleanup</c>; failures are logged and the service keeps running. Registered by
/// <see cref="Extensions.IdempotencyServiceCollectionExtensions.AddChassisHttpIdempotency(Microsoft.Extensions.DependencyInjection.IServiceCollection, QLS.Chassis.Core.Idempotency.IHttpIdempotencyPolicy, HttpIdempotencyCleanupPolicy)"/>.
/// </remarks>
public sealed class HttpIdempotencyCleanupHostedService : BackgroundService
{
    private readonly ChassisDatabaseOptions _databaseOptions;
    private readonly ResilienceOptions _databaseResilienceOptions;
    private readonly HttpIdempotencyCleanupPolicy _idempotencyCleanupPolicy;
    private readonly IDatabaseResilienceExecutor _resilienceExecutor;
    private readonly IStructuredAppLogger _structuredAppLogger;
    private readonly HttpIdempotencyStorePolicy _storePolicy;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="databaseOptions">The Postgres connection string.</param>
    /// <param name="databaseResilienceOptions">The database resilience policy; its timeout sets the command timeout.</param>
    /// <param name="idempotencyCleanupPolicy">How often to clean up and how many records to delete per run.</param>
    /// <param name="storePolicy">The schema and table that hold the records.</param>
    /// <param name="resilienceExecutor">Runs the delete with timeouts, retries and the circuit breaker.</param>
    /// <param name="structuredAppLogger">Logs each cleanup run.</param>
    /// <exception cref="InvalidOperationException">
    /// The schema or table name in <paramref name="storePolicy"/> is not a valid identifier.
    /// </exception>
    public HttpIdempotencyCleanupHostedService(
        ChassisDatabaseOptions databaseOptions,
        ResilienceOptions databaseResilienceOptions,
        HttpIdempotencyCleanupPolicy idempotencyCleanupPolicy,
        HttpIdempotencyStorePolicy storePolicy,
        IDatabaseResilienceExecutor resilienceExecutor,
        IStructuredAppLogger structuredAppLogger)
    {
        _databaseOptions = databaseOptions;
        _databaseResilienceOptions = databaseResilienceOptions;
        _idempotencyCleanupPolicy = idempotencyCleanupPolicy;
        _storePolicy = storePolicy;
        _resilienceExecutor = resilienceExecutor;
        _structuredAppLogger = structuredAppLogger;

        ValidateStorePolicy(storePolicy);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = _idempotencyCleanupPolicy.CleanupInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var deleted = await DeleteExpiredRecordsAsync(stoppingToken);
                if (deleted > 0)
                {
                    _structuredAppLogger.LogOperationSucceeded(
                        operation: "http_idempotency_cleanup",
                        statusCode: 200,
                        operationMetadata: new Dictionary<string, string?>
                        {
                            ["deletedCount"] = deleted.ToString(),
                        });
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _structuredAppLogger.LogOperationFailed(
                    operation: "http_idempotency_cleanup",
                    exception: ex,
                    statusCode: 500);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task<int> DeleteExpiredRecordsAsync(CancellationToken cancellationToken)
    {
        var table = _storePolicy.QualifiedTableName;
        var sql = @"
            DELETE FROM " + table + @"
            WHERE idempotency_record_key IN (
                SELECT idempotency_record_key
                FROM " + table + @"
                WHERE expires_at_utc < NOW()
                ORDER BY expires_at_utc
                LIMIT @BatchSize
            );";

        return await _resilienceExecutor.ExecuteAsync(async ct =>
        {
            await using var connection = new NpgsqlConnection(_databaseOptions.ConnectionString);
            return await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { BatchSize = _idempotencyCleanupPolicy.CleanupBatchSize },
                commandTimeout: Math.Max(1, (int)Math.Ceiling(_databaseResilienceOptions.TimeoutMs / 1000d)),
                cancellationToken: ct));
        }, cancellationToken);
    }

    private static void ValidateStorePolicy(HttpIdempotencyStorePolicy storePolicy)
    {
        if (!HttpIdempotencyStorePolicy.IsValidIdentifier(storePolicy.SchemaName)
            || !HttpIdempotencyStorePolicy.IsValidIdentifier(storePolicy.TableName))
        {
            throw new InvalidOperationException("Invalid HTTP idempotency store schema/table policy.");
        }
    }
}
