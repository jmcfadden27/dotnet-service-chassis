using Microsoft.AspNetCore.Http;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Middleware;

/// <summary>
/// Middleware that captures the request body and logs a <c>request_succeeded</c> event after each request that
/// completes.
/// </summary>
/// <remarks>
/// For methods that can have a body (anything but GET, HEAD, DELETE, TRACE and OPTIONS) the body is read into memory
/// first and kept in <see cref="HttpContext.Items"/> for error logs and idempotency; the request stream is rewound so
/// the endpoint can still read it. When the rest of the pipeline throws, nothing is logged here; the exception handler
/// logs the failure. Added by <see cref="Extensions.ApplicationBuilderExtensions.UseChassisWebApi"/>.
/// </remarks>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Captures the request body, runs the rest of the pipeline and logs the result.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="requestLogger">Writes the request log event.</param>
    /// <returns>A task that completes when the rest of the pipeline has run and the event is logged.</returns>
    public async Task InvokeAsync(HttpContext context, IWebApiRequestLogger requestLogger)
    {
        context.Items[RequestLoggingConstants.RequestBodyItemKey] = await TryCaptureBodyAsync(context);

        await _next(context);

        requestLogger.LogSuccess(context);
    }

    private static async Task<string?> TryCaptureBodyAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsDelete(context.Request.Method)
            || HttpMethods.IsTrace(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method))
        {
            return null;
        }

        if (context.Request.Body == Stream.Null)
            return null;

        context.Request.EnableBuffering();

        if (context.Request.Body.CanSeek)
            context.Request.Body.Position = 0;

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        if (context.Request.Body.CanSeek)
            context.Request.Body.Position = 0;

        return body;
    }
}
