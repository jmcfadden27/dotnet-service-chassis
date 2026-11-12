using QLS.Chassis.WebApi.Exceptions;
using QLS.Chassis.WebApi.Idempotency;

namespace QLS.Chassis.WebApi.Sample.Orders;

internal static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app, bool idempotentCreate)
    {
        var orders = app.MapGroup("/orders").WithTags("Orders");

        // A missing order throws KeyNotFoundException; the chassis turns it into a 404 problem details response.
        orders.MapGet("/{id}", async (string id, IOrderStore store, CancellationToken ct) =>
            Results.Ok(await store.GetAsync(id, ct) ?? throw new KeyNotFoundException($"Order '{id}' was not found.")));

        var create = orders.MapPost("/", async (CreateOrderRequest request, IOrderStore store, CancellationToken ct) =>
        {
            // ApiException sets the status and title of the problem details response.
            if (request.Quantity <= 0)
                throw new ApiException(StatusCodes.Status400BadRequest, "Invalid order", "Quantity must be at least 1.");

            var order = await store.AddAsync(new Order(Guid.NewGuid().ToString("n")[..8], request.Sku, request.Quantity), ct);
            return Results.Created($"/orders/{order.Id}", order);
        });

        // [HttpIdempotent] for minimal APIs: a retried request with the same Idempotency-Key gets the stored response.
        if (idempotentCreate)
            create.WithMetadata(new HttpIdempotentAttribute());
    }
}

internal sealed record CreateOrderRequest(string Sku, int Quantity);

internal sealed record Order(string Id, string Sku, int Quantity);
