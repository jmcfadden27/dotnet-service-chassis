// An orders API on the chassis. Two ways to run it (see README.md, "Run the samples"):
//
//   dotnet run --project samples/QLS.Chassis.WebApi.Sample
//     No database: error handling, correlation IDs, structured request logs, health endpoints and Swagger.
//
//   docker compose -f samples/QLS.Chassis.WebApi.Sample/docker-compose.yml up -d
//   dotnet run --project samples/QLS.Chassis.WebApi.Sample --launch-profile postgres
//     Adds Postgres resilience, a Postgres readiness check and an idempotent POST /orders.
using QLS.Chassis.WebApi.Data;
using QLS.Chassis.WebApi.Extensions;
using QLS.Chassis.WebApi.Idempotency;
using QLS.Chassis.WebApi.Sample.Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddChassisJwtSecurity());

// Errors as problem details, correlation IDs, structured request logging, current-user services.
builder.Services.AddChassisWebApi();

// With a connection string (the "postgres" launch profile sets one), orders live in Postgres and POST /orders is
// idempotent. Without one, orders are kept in memory.
var usePostgres = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("AppDb"));
if (usePostgres)
{
    builder.Services.AddChassisPostgresDatabaseResiliency(builder.Configuration, "AppDb", DatabaseResiliencePolicies.Default);
    builder.Services.AddChassisHttpIdempotency(HttpIdempotencyPolicies.Default, HttpIdempotencyCleanupPolicies.Default);
    builder.Services.AddSingleton<IOrderStore, PostgresOrderStore>();
}
else
{
    builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Exception handler, correlation ID, request logging and idempotency, in that order. (A real API with JWT
// authentication calls app.UseAuthentication() just before this; see docs/web-api.md.)
app.UseChassisWebApi();

app.MapOrderEndpoints(idempotentCreate: usePostgres);
app.MapChassisHealthEndpoints();

app.Logger.LogInformation("Orders are stored {Store}", usePostgres ? "in Postgres" : "in memory");
app.Run();
