using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using QLS.Chassis.WebApi.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace QLS.Chassis.WebApi.Extensions;

/// <summary>
/// Extension methods that add the chassis middleware and endpoints to an application.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the chassis middleware, in this order: the exception handler, <see cref="CorrelationIdMiddleware"/>,
    /// <see cref="RequestLoggingMiddleware"/> and <see cref="HttpIdempotencyMiddleware"/>.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="ServiceCollectionExtensions.AddChassisWebApi"/>. Call <c>UseAuthentication()</c> before this
    /// method, so request logs and idempotency records know the user, and <c>UseAuthorization()</c> after it.
    /// </remarks>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder, for chaining.</returns>
    public static IApplicationBuilder UseChassisWebApi(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<HttpIdempotencyMiddleware>();

        return app;
    }

    /// <summary>
    /// Maps two anonymous health endpoints: <c>/health/live</c>, which runs no checks and is healthy whenever the process
    /// answers, and <c>/health/ready</c>, which runs every health check tagged <c>ready</c>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapChassisHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            })
            .AllowAnonymous();

        endpoints
            .MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready")
            })
            .AllowAnonymous();

        return endpoints;
    }
}
