namespace QLS.Chassis.Core.Logging;

/// <summary>
/// The <see cref="StructuredLogEvent.EventName"/> values for the standard events.
/// </summary>
public static class LogEventNames
{
    /// <summary>
    /// An HTTP request completed successfully.
    /// </summary>
    public const string RequestSucceeded = "request_succeeded";
    /// <summary>
    /// An HTTP request failed.
    /// </summary>
    public const string RequestFailed = "request_failed";
    /// <summary>
    /// An operation, such as handling a message, completed successfully.
    /// </summary>
    public const string OperationSucceeded = "operation_succeeded";
    /// <summary>
    /// An operation failed.
    /// </summary>
    public const string OperationFailed = "operation_failed";
    /// <summary>
    /// An operation hit a problem worth a warning.
    /// </summary>
    public const string OperationWarning = "operation_warning";
}
