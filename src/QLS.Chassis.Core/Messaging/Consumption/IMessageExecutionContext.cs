namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Tells the consumer which service is running, the current request ID, and where to write logs.
/// </summary>
public interface IMessageExecutionContext
{
    /// <summary>
    /// The consuming service's name, used in log events and as the idempotency consumer name.
    /// </summary>
    string ServiceName { get; }
    /// <summary>
    /// The ID of the current invocation, for example a Lambda request ID.
    /// </summary>
    string RequestId { get; }
    /// <summary>
    /// Writes an information log line.
    /// </summary>
    /// <param name="message">The line to write.</param>
    void LogInformation(string message);
    /// <summary>
    /// Writes an error log line.
    /// </summary>
    /// <param name="message">The line to write.</param>
    void LogError(string message);
}
