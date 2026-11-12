using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace QLS.Chassis.WebApi.Middleware;

/// <summary>
/// Middleware that gives every request a correlation ID.
/// </summary>
/// <remarks>
/// Reads the <c>X-Correlation-Id</c> request header, or creates a new GUID when it is missing or blank. It then sets
/// <see cref="HttpContext.TraceIdentifier"/> to it, returns it in the <c>X-Correlation-Id</c> response header, and
/// opens an <see cref="ILogger"/> scope with a <c>CorrelationId</c> value for the rest of the pipeline. Added by
/// <see cref="Extensions.ApplicationBuilderExtensions.UseChassisWebApi"/>.
/// </remarks>
public sealed class CorrelationIdMiddleware
{
    /// <summary>
    /// The header the correlation ID is read from and returned in: <c>X-Correlation-Id</c>.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Sets the request's correlation ID and runs the rest of the pipeline.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <returns>A task that completes when the rest of the pipeline has run.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("n");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (context.RequestServices
                   .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
                   .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
