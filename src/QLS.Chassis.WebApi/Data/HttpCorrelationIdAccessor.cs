using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.WebApi.Data;

/// <summary>
/// <see cref="ICorrelationIdAccessor"/> that exposes the current HTTP request's correlation ID to QLS.Chassis.Core, so
/// resilience logs carry it.
/// </summary>
public sealed class HttpCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Creates the accessor.
    /// </summary>
    /// <param name="httpContextAccessor">Gives access to the current request.</param>
    public HttpCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns the current request's <see cref="HttpContext.TraceIdentifier"/> (set by
    /// <see cref="Middleware.CorrelationIdMiddleware"/>), or the trace ID of <see cref="Activity.Current"/> when there is
    /// no request, or <see langword="null"/> when there is neither.
    /// </remarks>
    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.TraceIdentifier
        ?? Activity.Current?.TraceId.ToString();
}
