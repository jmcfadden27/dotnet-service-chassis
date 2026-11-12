namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// An <see cref="IMessageExecutionContext"/> that writes logs through delegates, so it fits any host.
/// </summary>
public sealed class MessageExecutionContext : IMessageExecutionContext
{
    /// <inheritdoc />
    public required string ServiceName { get; init; }
    /// <inheritdoc />
    public required string RequestId { get; init; }
    /// <summary>
    /// Writes information log lines.
    /// </summary>
    public required Action<string> LogInfo { get; init; }
    /// <summary>
    /// Writes error log lines.
    /// </summary>
    public required Action<string> LogErr { get; init; }

    /// <inheritdoc />
    public void LogInformation(string message)
    {
        LogInfo(message);
    }

    /// <inheritdoc />
    public void LogError(string message)
    {
        LogErr(message);
    }
}
