namespace QLS.Chassis.WebApi.Data;

/// <summary>
/// Connection settings for the chassis Postgres components.
/// </summary>
/// <remarks>
/// Registered as a singleton by
/// <see cref="Extensions.DatabaseServiceCollectionExtensions.AddChassisPostgresDatabaseResiliency"/>, which fills it
/// from <c>ConnectionStrings:&lt;name&gt;</c>. Inject it to open your own connections.
/// </remarks>
public sealed class ChassisDatabaseOptions
{
    /// <summary>
    /// The Npgsql connection string. Defaults to an empty string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
