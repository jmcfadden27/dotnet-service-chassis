using System.Collections.Concurrent;
using Dapper;
using Npgsql;
using QLS.Chassis.WebApi.Data;

namespace QLS.Chassis.WebApi.Sample.Orders;

internal interface IOrderStore
{
    Task<Order?> GetAsync(string id, CancellationToken cancellationToken);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken);
}

internal sealed class InMemoryOrderStore : IOrderStore
{
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public Task<Order?> GetAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_orders.TryGetValue(id, out var order) ? order : null);

    public Task<Order> AddAsync(Order order, CancellationToken cancellationToken)
    {
        _orders[order.Id] = order;
        return Task.FromResult(order);
    }
}

/// <summary>
/// Orders in Postgres, every query through the chassis's <see cref="IDatabaseResilienceExecutor"/>: a timeout per
/// attempt, retries for transient errors, and a circuit breaker (503 while it's open).
/// </summary>
internal sealed class PostgresOrderStore(ChassisDatabaseOptions database, IDatabaseResilienceExecutor resilience) : IOrderStore
{
    public Task<Order?> GetAsync(string id, CancellationToken cancellationToken) =>
        resilience.ExecuteAsync(async attemptToken =>
        {
            // Open the connection inside the operation, so each retry gets a fresh one.
            await using var connection = new NpgsqlConnection(database.ConnectionString);
            return await connection.QuerySingleOrDefaultAsync<Order>(new CommandDefinition(
                "SELECT id, sku, quantity FROM orders WHERE id = @Id",
                new { Id = id },
                cancellationToken: attemptToken));
        }, cancellationToken);

    public Task<Order> AddAsync(Order order, CancellationToken cancellationToken) =>
        resilience.ExecuteAsync(async attemptToken =>
        {
            await using var connection = new NpgsqlConnection(database.ConnectionString);
            // ON CONFLICT DO NOTHING keeps a retried insert from failing on the order it already wrote.
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO orders (id, sku, quantity) VALUES (@Id, @Sku, @Quantity) ON CONFLICT (id) DO NOTHING",
                order,
                cancellationToken: attemptToken));
            return order;
        }, cancellationToken);
}
