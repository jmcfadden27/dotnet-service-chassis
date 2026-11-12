using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Logging;

namespace QLS.Chassis.WebApi.Observability;

/// <summary>
/// <see cref="IWebApiRequestLogger"/> that builds events with QLS.Chassis.Core's <see cref="IStructuredLogEventFactory"/>,
/// sanitizes failed requests with <see cref="ILogSanitizer"/> and writes them through
/// <see cref="ILogger{TCategoryName}"/>.
/// </summary>
/// <remarks>
/// The service name is <see cref="IHostEnvironment.ApplicationName"/>, or <c>web-api</c> when it is empty. The request
/// body is only logged when <see cref="Middleware.RequestLoggingMiddleware"/> captured it.
/// </remarks>
public sealed class WebApiRequestLogger : IWebApiRequestLogger
{
    private readonly ILogger<WebApiRequestLogger> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IStructuredLogEventFactory _logEventFactory;
    private readonly ILogSanitizer _logSanitizer;

    /// <summary>
    /// Creates the logger.
    /// </summary>
    /// <param name="logger">The logger the events are written to.</param>
    /// <param name="hostEnvironment">Supplies the service name.</param>
    /// <param name="logEventFactory">Builds the structured log events.</param>
    /// <param name="logSanitizer">Redacts and truncates request data in failure events.</param>
    public WebApiRequestLogger(
        ILogger<WebApiRequestLogger> logger,
        IHostEnvironment hostEnvironment,
        IStructuredLogEventFactory logEventFactory,
        ILogSanitizer logSanitizer)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _logEventFactory = logEventFactory;
        _logSanitizer = logSanitizer;
    }

    /// <inheritdoc />
    public void LogSuccess(HttpContext context)
    {
        var evt = _logEventFactory.CreateRequestSucceeded(
            ResolveServiceName(),
            context.TraceIdentifier,
            context.TraceIdentifier,
            ResolveTraceId(context),
            ResolveUserId(context),
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode);

        _logger.LogInformation("{StructuredEvent}", JsonSerializer.Serialize(evt));
    }

    /// <inheritdoc />
    public void LogError(HttpContext context, Exception exception, int statusCode)
    {
        var capturedBody = context.Items.TryGetValue(RequestLoggingConstants.RequestBodyItemKey, out var requestBodyObj)
            ? requestBodyObj as string
            : null;

        var sanitized = _logSanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = ToStringDictionary(context.Request.Headers),
            QueryStringParameters = ToStringDictionary(context.Request.Query),
            Body = capturedBody,
            IsBase64Encoded = false,
        });

        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["requestHeaders"] = sanitized.Headers,
            ["requestQuery"] = sanitized.QueryStringParameters,
            ["requestBody"] = sanitized.Body,
            ["requestBodyLogged"] = sanitized.BodyLogged,
            ["requestBodyReason"] = sanitized.BodyReason,
            ["redactionApplied"] = sanitized.RedactionApplied,
            ["truncated"] = sanitized.Truncated,
        };

        var evt = _logEventFactory.CreateRequestFailed(
            ResolveServiceName(),
            context.TraceIdentifier,
            context.TraceIdentifier,
            ResolveTraceId(context),
            ResolveUserId(context),
            context.Request.Method,
            context.Request.Path,
            statusCode,
            exception,
            metadata);

        _logger.LogError("{StructuredEvent}", JsonSerializer.Serialize(evt));
    }

    private string ResolveServiceName()
    {
        return string.IsNullOrWhiteSpace(_hostEnvironment.ApplicationName)
            ? "web-api"
            : _hostEnvironment.ApplicationName;
    }

    private static string? ResolveTraceId(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        return string.IsNullOrWhiteSpace(traceId) ? null : traceId;
    }

    private static string? ResolveUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst("sub")?.Value
            ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? user.FindFirst("cognito:username")?.Value
            ?? user.FindFirst("username")?.Value;
    }

    private static IReadOnlyDictionary<string, string>? ToStringDictionary(IHeaderDictionary headers)
    {
        if (headers.Count == 0)
            return null;

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
            values[header.Key] = header.Value.ToString();

        return values;
    }

    private static IReadOnlyDictionary<string, string>? ToStringDictionary(IQueryCollection query)
    {
        if (query.Count == 0)
            return null;

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query)
            values[pair.Key] = pair.Value.ToString();

        return values;
    }
}
