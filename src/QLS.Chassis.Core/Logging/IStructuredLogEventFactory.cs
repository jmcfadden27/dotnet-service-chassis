namespace QLS.Chassis.Core.Logging;

/// <summary>
/// Builds the standard <see cref="StructuredLogEvent"/>s that chassis components log.
/// </summary>
public interface IStructuredLogEventFactory
{
    /// <summary>
    /// Creates a <c>request_succeeded</c> event at Information. Its operation is <c>{method} {path}</c>, using <c>UNKNOWN</c> and <c>/</c> when they are null, and its metadata holds the method and path.
    /// </summary>
    /// <param name="service">The service writing the event.</param>
    /// <param name="requestId">The ID of the request or invocation.</param>
    /// <param name="correlationId">The correlation ID, if any.</param>
    /// <param name="traceId">The trace ID, if any.</param>
    /// <param name="userId">The user ID, if any.</param>
    /// <param name="httpMethod">The HTTP method, if known.</param>
    /// <param name="path">The request path, if known.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <returns>The event.</returns>
    StructuredLogEvent CreateRequestSucceeded(string service, string requestId, string? correlationId, string? traceId, string? userId, string? httpMethod, string? path, int statusCode);
    /// <summary>
    /// Creates a <c>request_failed</c> event at Error, with error fields from the exception. Its operation is <c>{method} {path}</c>, using <c>UNKNOWN</c> and <c>/</c> when they are null, and its metadata holds the method and path.
    /// </summary>
    /// <param name="service">The service writing the event.</param>
    /// <param name="requestId">The ID of the request or invocation.</param>
    /// <param name="correlationId">The correlation ID, if any.</param>
    /// <param name="traceId">The trace ID, if any.</param>
    /// <param name="userId">The user ID, if any.</param>
    /// <param name="httpMethod">The HTTP method, if known.</param>
    /// <param name="path">The request path, if known.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="exception">The exception, which sets the error fields.</param>
    /// <param name="additionalMetadata">Extra metadata, which overrides entries of the same name.</param>
    /// <returns>The event.</returns>
    StructuredLogEvent CreateRequestFailed(string service, string requestId, string? correlationId, string? traceId, string? userId, string? httpMethod, string? path, int statusCode, Exception exception, IReadOnlyDictionary<string, object?>? additionalMetadata = null);
    /// <summary>
    /// Creates an <c>operation_succeeded</c> event at Information.
    /// </summary>
    /// <param name="service">The service writing the event.</param>
    /// <param name="requestId">The ID of the request or invocation.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="correlationId">The correlation ID, if any.</param>
    /// <param name="traceId">The trace ID, if any.</param>
    /// <param name="userId">The user ID, if any.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="durationMs">How long the operation took, in milliseconds.</param>
    /// <param name="operationMetadata">Extra values for the event's metadata, if any.</param>
    /// <param name="messageId">The message ID, if the operation handled a message.</param>
    /// <param name="causationId">The causation ID, if the operation handled a message.</param>
    /// <returns>The event.</returns>
    StructuredLogEvent CreateOperationSucceeded(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId);
    /// <summary>
    /// Creates an <c>operation_failed</c> event at Error, with error fields from the exception.
    /// </summary>
    /// <param name="service">The service writing the event.</param>
    /// <param name="requestId">The ID of the request or invocation.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="correlationId">The correlation ID, if any.</param>
    /// <param name="traceId">The trace ID, if any.</param>
    /// <param name="userId">The user ID, if any.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="durationMs">How long the operation took, in milliseconds.</param>
    /// <param name="operationMetadata">Extra values for the event's metadata, if any.</param>
    /// <param name="messageId">The message ID, if the operation handled a message.</param>
    /// <param name="causationId">The causation ID, if the operation handled a message.</param>
    /// <param name="exception">The exception, which sets the error fields.</param>
    /// <returns>The event.</returns>
    StructuredLogEvent CreateOperationFailed(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId, Exception exception);
    /// <summary>
    /// Creates an <c>operation_warning</c> event at Warning, with error fields from the exception.
    /// </summary>
    /// <param name="service">The service writing the event.</param>
    /// <param name="requestId">The ID of the request or invocation.</param>
    /// <param name="operation">The operation name.</param>
    /// <param name="correlationId">The correlation ID, if any.</param>
    /// <param name="traceId">The trace ID, if any.</param>
    /// <param name="userId">The user ID, if any.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="durationMs">How long the operation took, in milliseconds.</param>
    /// <param name="operationMetadata">Extra values for the event's metadata, if any.</param>
    /// <param name="messageId">The message ID, if the operation handled a message.</param>
    /// <param name="causationId">The causation ID, if the operation handled a message.</param>
    /// <param name="exception">The exception, which sets the error fields.</param>
    /// <returns>The event.</returns>
    StructuredLogEvent CreateOperationWarning(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId, Exception exception);
}
