using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using QLS.Chassis.Core.Resilience;
using QLS.Chassis.WebApi.Data;

namespace QLS.Chassis.WebApi.Extensions;

/// <summary>
/// Extension methods that register the chassis Postgres resilience services.
/// </summary>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers resilient Postgres access: <see cref="ChassisDatabaseOptions"/>, <see cref="IDatabaseResilienceExecutor"/>,
    /// QLS.Chassis.Core's <see cref="IResilienceExecutor"/>, the <see cref="ResilienceOptions"/> policy,
    /// <see cref="HttpCorrelationIdAccessor"/> as <see cref="ICorrelationIdAccessor"/>, and a <c>postgres</c> health check
    /// tagged <c>ready</c>.
    /// </summary>
    /// <remarks>
    /// The registered policy is a copy of <paramref name="policy"/> whose transient-error check also treats
    /// <see cref="TimeoutException"/>, an <see cref="NpgsqlException"/> with <see cref="NpgsqlException.IsTransient"/> set,
    /// and <see cref="SocketException"/> as transient, including when wrapped in another exception. Resilience log events
    /// use <see cref="IHostEnvironment.ApplicationName"/> as the service name unless
    /// <see cref="ResilienceLoggingOptions"/> is already registered.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to read the connection string from.</param>
    /// <param name="connectionStringName">The name under <c>ConnectionStrings</c>, such as <c>AppDb</c>.</param>
    /// <param name="policy">The resilience policy, such as <see cref="DatabaseResiliencePolicies.Default"/>.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The connection string is missing from configuration.</exception>
    public static IServiceCollection AddChassisPostgresDatabaseResiliency(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName,
        ResilienceOptions policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Missing ConnectionStrings:{connectionStringName}");

        services.AddSingleton(new ChassisDatabaseOptions
        {
            ConnectionString = connectionString,
        });

        services.AddSingleton<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();
        // Resilience events carry the same service name as the API's other logs.
        services.TryAddSingleton(sp => new ResilienceLoggingOptions
        {
            ServiceName = sp.GetService<IHostEnvironment>()?.ApplicationName,
        });
        services.AddSingleton<IResilienceExecutor, ResilienceExecutor>();
        services.AddSingleton(CreatePostgresPolicy(policy));
        services.AddSingleton<IDatabaseResilienceExecutor, CoreResilienceDatabaseExecutor>();

        services.AddHealthChecks()
            .AddCheck<PostgresReadinessHealthCheck>("postgres", tags: ["ready"]);

        return services;
    }

    private static ResilienceOptions CreatePostgresPolicy(ResilienceOptions basePolicy)
    {
        return new ResilienceOptions
        {
            DependencyName = basePolicy.DependencyName,
            TimeoutMs = basePolicy.TimeoutMs,
            MaxRetries = basePolicy.MaxRetries,
            BaseDelayMs = basePolicy.BaseDelayMs,
            CircuitFailureRatio = basePolicy.CircuitFailureRatio,
            CircuitMinThroughput = basePolicy.CircuitMinThroughput,
            CircuitBreakSeconds = basePolicy.CircuitBreakSeconds,
            RetryAfterSeconds = basePolicy.RetryAfterSeconds,
            TransientExceptionClassifier = ex =>
                basePolicy.TransientExceptionClassifier?.Invoke(ex) == true
                || IsPostgresTransient(ex),
        };
    }

    private static bool IsPostgresTransient(Exception exception)
    {
        if (exception is TimeoutException)
            return true;

        if (exception is NpgsqlException npgsqlException && npgsqlException.IsTransient)
            return true;

        if (exception is SocketException)
            return true;

        return exception.InnerException != null && IsPostgresTransient(exception.InnerException);
    }
}
