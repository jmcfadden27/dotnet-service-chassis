using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using QLS.Chassis.Core.Idempotency;
using QLS.Chassis.WebApi.Auth;
using QLS.Chassis.WebApi.Data;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Exceptions;

/// <summary>
/// <see cref="IExceptionHandler"/> that logs an unhandled exception and turns it into an RFC 7807 problem details
/// response.
/// </summary>
/// <remarks>
/// <para>
/// Status codes and titles: <see cref="ApiException"/> uses its own; <see cref="UnauthorizedAccessException"/> and
/// <see cref="UserContextUnavailableException"/> give 401; <see cref="ValidationException"/> and
/// <see cref="IdempotencyKeyRequiredException"/> give 400; <see cref="IdempotencyKeyReuseConflictException"/> and
/// <see cref="IdempotencyRequestInProgressException"/> give 409; <see cref="KeyNotFoundException"/> gives 404;
/// <see cref="DatabaseCircuitOpenException"/> gives 503; anything else gives 500.
/// </para>
/// <para>
/// The response's detail is the exception's <see cref="Exception.Message"/> for every status, including 500, and
/// its instance is the request path. Registered by
/// <see cref="Extensions.ServiceCollectionExtensions.AddChassisWebApi"/>.
/// </para>
/// </remarks>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IWebApiRequestLogger _requestLogger;

    /// <summary>
    /// Creates the handler.
    /// </summary>
    /// <param name="problemDetailsService">Writes the problem details response.</param>
    /// <param name="requestLogger">Logs the failed request.</param>
    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        IWebApiRequestLogger requestLogger)
    {
        _problemDetailsService = problemDetailsService;
        _requestLogger = requestLogger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Logs the exception through <see cref="IWebApiRequestLogger.LogError"/>, sets the status code and returns whether
    /// the problem details response was written.
    /// </remarks>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ApiException apiException => (apiException.StatusCode, apiException.Title),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            UserContextUnavailableException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
            DatabaseCircuitOpenException => (StatusCodes.Status503ServiceUnavailable, "Service unavailable"),
            IdempotencyKeyRequiredException => (StatusCodes.Status400BadRequest, "Idempotency key required"),
            IdempotencyKeyReuseConflictException => (StatusCodes.Status409Conflict, "Idempotency key conflict"),
            IdempotencyRequestInProgressException => (StatusCodes.Status409Conflict, "Idempotency request in progress"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        _requestLogger.LogError(httpContext, exception, statusCode);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
