namespace QLS.Chassis.WebApi.Idempotency;

/// <summary>
/// The Postgres schema and table that hold HTTP idempotency records.
/// <see cref="HttpIdempotencyStorePolicies.Default"/> is <c>public.idempotency_records</c>.
/// </summary>
/// <remarks>
/// Both names must pass <see cref="IsValidIdentifier"/>; <see cref="PostgresHttpIdempotencyStore"/> and
/// <see cref="HttpIdempotencyCleanupHostedService"/> check them when they are created.
/// </remarks>
public sealed class HttpIdempotencyStorePolicy
{
    /// <summary>
    /// The schema name.
    /// </summary>
    public required string SchemaName { get; init; }
    /// <summary>
    /// The table name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// The table name qualified with its schema, as <c>schema.table</c> (unquoted).
    /// </summary>
    public string QualifiedTableName => $"{SchemaName}.{TableName}";

    /// <summary>
    /// Checks that a name is a plain identifier that is safe to put in SQL: not empty, starting with a letter or
    /// underscore, and containing only letters, digits and underscores.
    /// </summary>
    /// <param name="value">The name to check.</param>
    /// <returns><see langword="true"/> when the name is a valid identifier; otherwise <see langword="false"/>.</returns>
    public static bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Ready-made store policies.
/// </summary>
public static class HttpIdempotencyStorePolicies
{
    /// <summary>
    /// The <c>public.idempotency_records</c> table.
    /// </summary>
    public static readonly HttpIdempotencyStorePolicy Default = new()
    {
        SchemaName = "public",
        TableName = "idempotency_records",
    };
}
