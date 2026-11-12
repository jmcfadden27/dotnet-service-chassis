using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Logging;

namespace QLS.Chassis.WebApi.Observability;

/// <summary>
/// <see cref="IStructuredAppLogger"/> that builds events with QLS.Chassis.Core's <see cref="IStructuredLogEventFactory"/>
/// and writes them through <see cref="ILogger{TCategoryName}"/>.
/// </summary>
/// <remarks>
/// The service name is <see cref="IHostEnvironment.ApplicationName"/>, or <c>web-api</c> when it is empty. The
/// correlation ID and user ID come from the current request, when there is one.
/// </remarks>
public sealed class StructuredAppLogger : IStructuredAppLogger
{
    private readonly ILogger<StructuredAppLogger> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IStructuredLogEventFactory _logEventFactory;

    /// <summary>
    /// Creates the logger.
    /// </summary>
    /// <param name="logger">The logger the events are written to.</param>
    /// <param name="httpContextAccessor">Gives access to the current request.</param>
    /// <param name="hostEnvironment">Supplies the service name.</param>
    /// <param name="logEventFactory">Builds the structured log events.</param>
    public StructuredAppLogger(
        ILogger<StructuredAppLogger> logger,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment hostEnvironment,
        IStructuredLogEventFactory logEventFactory)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _hostEnvironment = hostEnvironment;
        _logEventFactory = logEventFactory;
    }

    /// <inheritdoc />
    public void LogOperationSucceeded(
        string operation,
        int statusCode,
        long? durationMs = null,
        IReadOnlyDictionary<string, string?>? operationMetadata = null,
        string? messageId = null,
        string? causationId = null)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation is required", nameof(operation));

        var context = _httpContextAccessor.HttpContext;
        var evt = _logEventFactory.CreateOperationSucceeded(
            ResolveServiceName(),
            context?.TraceIdentifier ?? string.Empty,
            operation,
            context?.TraceIdentifier,
            ResolveTraceId(),
            ResolveUserId(context),
            statusCode,
            durationMs ?? 0,
            operationMetadata,
            messageId,
            causationId);

        _logger.LogInformation("{StructuredEvent}", JsonSerializer.Serialize(evt));
    }

    /// <inheritdoc />
    public void LogOperationFailed(
        string operation,
        Exception exception,
        int statusCode,
        long? durationMs = null,
        IReadOnlyDictionary<string, string?>? operationMetadata = null,
        string? messageId = null,
        string? causationId = null)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation is required", nameof(operation));

        var context = _httpContextAccessor.HttpContext;
        var evt = _logEventFactory.CreateOperationFailed(
            ResolveServiceName(),
            context?.TraceIdentifier ?? string.Empty,
            operation,
            context?.TraceIdentifier,
            ResolveTraceId(),
            ResolveUserId(context),
            statusCode,
            durationMs ?? 0,
            operationMetadata,
            messageId,
            causationId,
            exception);

        _logger.LogError("{StructuredEvent}", JsonSerializer.Serialize(evt));
    }

    /// <inheritdoc />
    public void LogOperationWarning(
        string operation,
        Exception exception,
        int statusCode,
        long? durationMs = null,
        IReadOnlyDictionary<string, string?>? operationMetadata = null,
        string? messageId = null,
        string? causationId = null)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Operation is required", nameof(operation));

        var context = _httpContextAccessor.HttpContext;
        var evt = _logEventFactory.CreateOperationWarning(
            ResolveServiceName(),
            context?.TraceIdentifier ?? string.Empty,
            operation,
            context?.TraceIdentifier,
            ResolveTraceId(),
            ResolveUserId(context),
            statusCode,
            durationMs ?? 0,
            operationMetadata,
            messageId,
            causationId,
            exception);

        _logger.LogWarning("{StructuredEvent}", JsonSerializer.Serialize(evt));
    }

    private string ResolveServiceName()
    {
        return string.IsNullOrWhiteSpace(_hostEnvironment.ApplicationName)
            ? "web-api"
            : _hostEnvironment.ApplicationName;
    }

    private static string? ResolveTraceId()
    {
        var traceId = Activity.Current?.TraceId.ToString();
        return string.IsNullOrWhiteSpace(traceId) ? null : traceId;
    }

    private static string? ResolveUserId(HttpContext? context)
    {
        var user = context?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("cognito:username")
            ?? user.FindFirstValue("username");
    }

}
