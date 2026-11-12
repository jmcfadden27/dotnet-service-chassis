namespace QLS.Chassis.WebApi.Observability;

/// <summary>
/// Writes structured <c>operation_*</c> log events for application code, with the current request's correlation ID,
/// trace ID and user ID filled in.
/// </summary>
/// <remarks>
/// Each call writes one QLS.Chassis.Core structured log event as JSON through <see cref="Microsoft.Extensions.Logging.ILogger"/>.
/// Registered as a singleton by <see cref="Extensions.ServiceCollectionExtensions.AddChassisWebApi"/>.
/// </remarks>
public interface IStructuredAppLogger
{
    /// <summary>
    /// Logs a successful operation at Information level.
    /// </summary>
    /// <param name="operation">The operation's name, such as <c>orders.ship</c>. Required.</param>
    /// <param name="statusCode">The status code that describes the outcome.</param>
    /// <param name="durationMs">How long the operation took, in milliseconds. Logged as 0 when <see langword="null"/>.</param>
    /// <param name="operationMetadata">Extra values to include in the event.</param>
    /// <param name="messageId">The ID of the message being processed, if any.</param>
    /// <param name="causationId">The ID of the message or event that caused this operation, if any.</param>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is null, empty or whitespace.</exception>
    void LogOperationSucceeded(
        string operation,
        int statusCode,
        long? durationMs = null,
        IReadOnlyDictionary<string, string?>? operationMetadata = null,
        string? messageId = null,
        string? causationId = null);

    /// <summary>
    /// Logs a failed operation at Error level.
    /// </summary>
    /// <param name="operation">The operation's name, such as <c>orders.ship</c>. Required.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="statusCode">The status code that describes the outcome.</param>
    /// <param name="durationMs">How long the operation took, in milliseconds. Logged as 0 when <see langword="null"/>.</param>
    /// <param name="operationMetadata">Extra values to include in the event.</param>
    /// <param name="messageId">The ID of the message being processed, if any.</param>
    /// <param name="causationId">The ID of the message or event that caused this operation, if any.</param>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is null, empty or whitespace.</exception>
    void LogOperationFailed(
        string operation,
        Exception exception,
        int statusCode,
        long? durationMs = null,
        IReadOnlyDictionary<string, string?>? operationMetadata = null,
        string? messageId = null,
        string? causationId = null);

    /// <summary>
    /// Logs an operation that completed with a problem at Warning level.
    /// </summary>
    /// <param name="operation">The operation's name, such as <c>orders.ship</c>. Required.</param>
    /// <param name="exception">The exception that describes the problem.</param>
    /// <param name="statusCode">The status code that describes the outcome.</param>
    /// <param name="durationMs">How long the operation took, in milliseconds. Logged as 0 when <see langword="null"/>.</param>
    /// <param name="operationMetadata">Extra values to include in the event.</param>
    /// <param name="messageId">The ID of the message being processed, if any.</param>
    /// <param name="causationId">The ID of the message or event that caused this operation, if any.</param>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is null, empty or whitespace.</exception>
    void LogOperationWarning(
        string operation,
        Exception exception,
        int statusCode,
        long? durationMs = null,
        IReadOnlyDictionary<string, string?>? operationMetadata = null,
        string? messageId = null,
        string? causationId = null);
}
