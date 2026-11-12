namespace QLS.Chassis.Core.Logging;

/// <summary>
/// Default <see cref="IStructuredLogEventFactory"/>. Failure events get an <see cref="StructuredLogEvent.ErrorCategory"/>
/// from the exception type and status code, and the exception text cut at 16 KB.
/// </summary>
public sealed class DefaultStructuredLogEventFactory : IStructuredLogEventFactory
{
    private const int MaxErrorStackTraceLength = 16 * 1024;

    /// <inheritdoc />
    public StructuredLogEvent CreateRequestSucceeded(string service, string requestId, string? correlationId, string? traceId, string? userId, string? httpMethod, string? path, int statusCode)
    {
        return new StructuredLogEvent
        {
            Level = "Information",
            EventName = LogEventNames.RequestSucceeded,
            Service = service,
            Operation = BuildRequestOperation(httpMethod, path),
            CorrelationId = correlationId,
            TraceId = traceId,
            RequestId = requestId,
            UserId = userId,
            StatusCode = statusCode,
            Metadata = new Dictionary<string, object?>
            {
                ["httpMethod"] = httpMethod,
                ["path"] = path,
            }
        };
    }

    /// <inheritdoc />
    public StructuredLogEvent CreateRequestFailed(string service, string requestId, string? correlationId, string? traceId, string? userId, string? httpMethod, string? path, int statusCode, Exception exception, IReadOnlyDictionary<string, object?>? additionalMetadata = null)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["httpMethod"] = httpMethod,
            ["path"] = path,
        };

        if (additionalMetadata != null)
        {
            foreach (var pair in additionalMetadata)
                metadata[pair.Key] = pair.Value;
        }

        return new StructuredLogEvent
        {
            Level = "Error",
            EventName = LogEventNames.RequestFailed,
            Service = service,
            Operation = BuildRequestOperation(httpMethod, path),
            CorrelationId = correlationId,
            TraceId = traceId,
            RequestId = requestId,
            UserId = userId,
            StatusCode = statusCode,
            ErrorCategory = ClassifyErrorCategory(statusCode, exception),
            ErrorType = exception.GetType().Name,
            ErrorMessage = exception.Message,
            ErrorStackTrace = Truncate(exception.ToString(), MaxErrorStackTraceLength),
            Metadata = metadata,
        };
    }

    /// <inheritdoc />
    public StructuredLogEvent CreateOperationSucceeded(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId)
    {
        return new StructuredLogEvent
        {
            Level = "Information",
            EventName = LogEventNames.OperationSucceeded,
            Service = service,
            Operation = operation,
            CorrelationId = correlationId,
            TraceId = traceId,
            RequestId = requestId,
            UserId = userId,
            MessageId = messageId,
            CausationId = causationId,
            StatusCode = statusCode,
            DurationMs = durationMs,
            Metadata = ToObjectDictionary(operationMetadata),
        };
    }

    /// <inheritdoc />
    public StructuredLogEvent CreateOperationFailed(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId, Exception exception)
    {
        return new StructuredLogEvent
        {
            Level = "Error",
            EventName = LogEventNames.OperationFailed,
            Service = service,
            Operation = operation,
            CorrelationId = correlationId,
            TraceId = traceId,
            RequestId = requestId,
            UserId = userId,
            MessageId = messageId,
            CausationId = causationId,
            StatusCode = statusCode,
            DurationMs = durationMs,
            ErrorCategory = ClassifyErrorCategory(statusCode, exception),
            ErrorType = exception.GetType().Name,
            ErrorMessage = exception.Message,
            ErrorStackTrace = Truncate(exception.ToString(), MaxErrorStackTraceLength),
            Metadata = ToObjectDictionary(operationMetadata),
        };
    }

    /// <inheritdoc />
    public StructuredLogEvent CreateOperationWarning(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId, Exception exception)
    {
        return new StructuredLogEvent
        {
            Level = "Warning",
            EventName = LogEventNames.OperationWarning,
            Service = service,
            Operation = operation,
            CorrelationId = correlationId,
            TraceId = traceId,
            RequestId = requestId,
            UserId = userId,
            MessageId = messageId,
            CausationId = causationId,
            StatusCode = statusCode,
            DurationMs = durationMs,
            ErrorCategory = ClassifyErrorCategory(statusCode, exception),
            ErrorType = exception.GetType().Name,
            ErrorMessage = exception.Message,
            ErrorStackTrace = Truncate(exception.ToString(), MaxErrorStackTraceLength),
            Metadata = ToObjectDictionary(operationMetadata),
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }

    private static string BuildRequestOperation(string? httpMethod, string? path)
    {
        return $"{httpMethod ?? "UNKNOWN"} {path ?? "/"}";
    }

    private static IReadOnlyDictionary<string, object?>? ToObjectDictionary(IReadOnlyDictionary<string, string?>? values)
    {
        if (values == null)
            return null;

        return values.ToDictionary(k => k.Key, v => (object?)v.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string ClassifyErrorCategory(int statusCode, Exception exception)
    {
        if (exception.GetType().Name.Contains("Idempotency", StringComparison.OrdinalIgnoreCase))
            return "idempotency";

        if (statusCode == 401)
            return "authentication";
        if (statusCode == 403)
            return "authorization";
        if (statusCode == 404)
            return "not_found";
        if (statusCode == 409)
            return "conflict";
        if (statusCode == 429)
            return "rate_limited";
        if (statusCode >= 400 && statusCode < 500)
            return "validation";
        if (statusCode >= 500)
            return "dependency_failure";

        return "unexpected";
    }
}
