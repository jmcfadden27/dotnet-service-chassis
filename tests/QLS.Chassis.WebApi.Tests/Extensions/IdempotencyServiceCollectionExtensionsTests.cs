using Microsoft.Extensions.DependencyInjection;
using QLS.Chassis.Core.Idempotency;
using QLS.Chassis.WebApi.Extensions;
using QLS.Chassis.WebApi.Idempotency;

namespace QLS.Chassis.WebApi.Tests.Extensions;

public sealed class IdempotencyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddChassisHttpIdempotency_RegistersExpectedServices()
    {
        var services = new ServiceCollection();

        services.AddChassisHttpIdempotency(HttpIdempotencyPolicies.Default, HttpIdempotencyCleanupPolicies.Default);

        Assert.Contains(services, d => d.ServiceType == typeof(IHttpIdempotencyPolicy));
        Assert.Contains(services, d => d.ServiceType == typeof(HttpIdempotencyCleanupPolicy));
        Assert.Contains(services, d => d.ServiceType == typeof(HttpIdempotencyStorePolicy));
        Assert.Contains(services, d => d.ServiceType == typeof(IIdempotencyStore) && d.ImplementationType == typeof(PostgresHttpIdempotencyStore));
        Assert.Contains(services, d => d.ServiceType == typeof(IHttpIdempotencyCoordinator) && d.ImplementationType == typeof(HttpIdempotencyCoordinator));
        Assert.Contains(services, d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) && d.ImplementationType == typeof(HttpIdempotencyCleanupHostedService));
    }

    [Fact]
    public void AddChassisHttpIdempotency_RegistersCustomStorePolicy_WhenProvided()
    {
        var services = new ServiceCollection();
        var custom = new HttpIdempotencyStorePolicy
        {
            SchemaName = "sample_app",
            TableName = "idempotency_records",
        };

        services.AddChassisHttpIdempotency(HttpIdempotencyPolicies.Default, HttpIdempotencyCleanupPolicies.Default, custom);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<HttpIdempotencyStorePolicy>();

        Assert.Equal("sample_app", resolved.SchemaName);
        Assert.Equal("idempotency_records", resolved.TableName);
    }
}
