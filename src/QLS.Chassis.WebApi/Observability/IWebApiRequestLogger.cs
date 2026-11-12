using Microsoft.AspNetCore.Http;

namespace QLS.Chassis.WebApi.Observability;

/// <summary>
/// Writes one structured log event per HTTP request.
/// </summary>
/// <remarks>
/// Registered as a singleton by <see cref="Extensions.ServiceCollectionExtensions.AddChassisWebApi"/> and used by
/// <see cref="Middleware.RequestLoggingMiddleware"/> and <see cref="Exceptions.GlobalExceptionHandler"/>.
/// </remarks>
public interface IWebApiRequestLogger
{
    /// <summary>
    /// Logs a <c>request_succeeded</c> event with the request's method, path, response status code, user ID and
    /// correlation ID.
    /// </summary>
    /// <param name="context">The completed request.</param>
    void LogSuccess(HttpContext context);
    /// <summary>
    /// Logs a <c>request_failed</c> event with the exception and the request's headers, query string and captured body,
    /// sanitized to redact secrets and truncate long values.
    /// </summary>
    /// <param name="context">The failed request.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="statusCode">The status code returned to the client.</param>
    void LogError(HttpContext context, Exception exception, int statusCode);
}
