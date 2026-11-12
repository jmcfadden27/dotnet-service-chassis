using Dapper;
using Newtonsoft.Json;
using Npgsql;
using QLS.Chassis.Core.Idempotency;
using QLS.Chassis.Core.Resilience;
using QLS.Chassis.WebApi.Data;

namespace QLS.Chassis.WebApi.Idempotency;

/// <summary>
/// <see cref="IIdempotencyStore"/> that keeps HTTP idempotency records in a Postgres table, using Dapper through
/// <see cref="IDatabaseResilienceExecutor"/>.
/// </summary>
/// <remarks>
/// The table is named by <see cref="HttpIdempotencyStorePolicy"/> and must already exist; docs/web-api.md has the
/// expected schema. Stored responses are serialized with Newtonsoft.Json. Database calls are not cancellable by the
/// caller; only the per-attempt timeout applies.
/// </remarks>
public sealed class PostgresHttpIdempotencyStore : IIdempotencyStore
{
    private const string StatusInProgress = "IN_PROGRESS";
    private const string StatusCompleted = "COMPLETED";

    private readonly ChassisDatabaseOptions _databaseOptions;
    private readonly ResilienceOptions _databaseResilienceOptions;
    private readonly IDatabaseResilienceExecutor _resilienceExecutor;
    private readonly HttpIdempotencyStorePolicy _storePolicy;

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="databaseOptions">The Postgres connection string.</param>
    /// <param name="databaseResilienceOptions">The database resilience policy; its timeout sets the command timeout.</param>
    /// <param name="resilienceExecutor">Runs each query with timeouts, retries and the circuit breaker.</param>
    /// <param name="storePolicy">The schema and table that hold the records.</param>
    /// <exception cref="InvalidOperationException">
    /// The schema or table name in <paramref name="storePolicy"/> is not a valid identifier.
    /// </exception>
    public PostgresHttpIdempotencyStore(
        ChassisDatabaseOptions databaseOptions,
        ResilienceOptions databaseResilienceOptions,
        IDatabaseResilienceExecutor resilienceExecutor,
        HttpIdempotencyStorePolicy storePolicy)
    {
        _databaseOptions = databaseOptions;
        _databaseResilienceOptions = databaseResilienceOptions;
        _resilienceExecutor = resilienceExecutor;
        _storePolicy = storePolicy;

        ValidateStorePolicy(storePolicy);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Inserts an <c>IN_PROGRESS</c> record, or takes over an existing record only when it has expired. Returns
    /// <see langword="true"/> when a row was inserted or taken over.
    /// </remarks>
    public async Task<bool> TryClaimAsync(string key, string fingerprint, long expiresAtEpochSeconds, long nowEpochSeconds)
    {
        // One statement, so the claim is atomic: insert a new in-progress record, or take over an existing one only if
        // it has expired. A live record (in progress or completed) is left alone and no row is affected.
        var table = _storePolicy.QualifiedTableName;
        var sql = @"
            INSERT INTO " + table + @" (
                idempotency_record_key,
                status,
                fingerprint,
                expires_at_utc,
                created_utc,
                updated_utc)
            VALUES (
                @Key,
                @InProgressStatus,
                @Fingerprint,
                @ExpiresAtUtc,
                NOW(),
                NOW())
            ON CONFLICT (idempotency_record_key)
            DO UPDATE SET
                status = EXCLUDED.status,
                fingerprint = EXCLUDED.fingerprint,
                response_json = NULL,
                expires_at_utc = EXCLUDED.expires_at_utc,
                updated_utc = NOW()
            WHERE " + table + @".expires_at_utc < @NowUtc;";

        var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expiresAtEpochSeconds).UtcDateTime;
        var nowUtc = DateTimeOffset.FromUnixTimeSeconds(nowEpochSeconds).UtcDateTime;

        return await _resilienceExecutor.ExecuteAsync(async ct =>
        {
            await using var connection = new NpgsqlConnection(_databaseOptions.ConnectionString);
            var affected = await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    Key = key,
                    InProgressStatus = StatusInProgress,
                    Fingerprint = fingerprint,
                    ExpiresAtUtc = expiresAtUtc,
                    NowUtc = nowUtc,
                },
                commandTimeout: Math.Max(1, (int)Math.Ceiling(_databaseResilienceOptions.TimeoutMs / 1000d)),
                cancellationToken: ct));

            return affected > 0;
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IdempotencyStoreRecord?> LoadAsync(string key)
    {
        var table = _storePolicy.QualifiedTableName;
        var sql = @"
            SELECT
                status AS Status,
                fingerprint AS Fingerprint,
                COALESCE(response_json, '') AS ResponseJson
            FROM " + table + @"
            WHERE idempotency_record_key = @Key;";

        return await _resilienceExecutor.ExecuteAsync(async ct =>
        {
            await using var connection = new NpgsqlConnection(_databaseOptions.ConnectionString);
            return await connection.QuerySingleOrDefaultAsync<IdempotencyStoreRecord>(new CommandDefinition(
                sql,
                new { Key = key },
                commandTimeout: Math.Max(1, (int)Math.Ceiling(_databaseResilienceOptions.TimeoutMs / 1000d)),
                cancellationToken: ct));
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Marks the record <c>COMPLETED</c> and stores the response as JSON with the new expiry.
    /// </remarks>
    public async Task SaveCompletedAsync(string key, string fingerprint, HttpIdempotencyExecutionResponse response, long expiresAtEpochSeconds)
    {
        var table = _storePolicy.QualifiedTableName;
        var sql = @"
            UPDATE " + table + @"
            SET
                status = @CompletedStatus,
                fingerprint = @Fingerprint,
                response_json = @ResponseJson,
                expires_at_utc = @ExpiresAtUtc,
                updated_utc = NOW()
            WHERE idempotency_record_key = @Key;";

        var responseJson = JsonConvert.SerializeObject(response);
        var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expiresAtEpochSeconds).UtcDateTime;

        await _resilienceExecutor.ExecuteAsync(async ct =>
        {
            await using var connection = new NpgsqlConnection(_databaseOptions.ConnectionString);
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    Key = key,
                    CompletedStatus = StatusCompleted,
                    Fingerprint = fingerprint,
                    ResponseJson = responseJson,
                    ExpiresAtUtc = expiresAtUtc,
                },
                commandTimeout: Math.Max(1, (int)Math.Ceiling(_databaseResilienceOptions.TimeoutMs / 1000d)),
                cancellationToken: ct));
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key)
    {
        var table = _storePolicy.QualifiedTableName;
        var sql = @"
            DELETE FROM " + table + @"
            WHERE idempotency_record_key = @Key;";

        await _resilienceExecutor.ExecuteAsync(async ct =>
        {
            await using var connection = new NpgsqlConnection(_databaseOptions.ConnectionString);
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { Key = key },
                commandTimeout: Math.Max(1, (int)Math.Ceiling(_databaseResilienceOptions.TimeoutMs / 1000d)),
                cancellationToken: ct));
        }, CancellationToken.None);
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
