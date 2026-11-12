namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// The outcome of a batch run by <see cref="IMessageConsumerExecutor"/>.
/// </summary>
public sealed class MessageConsumerExecutionResult
{
    /// <summary>
    /// The transport message IDs of the records that failed, for example for an SQS partial batch response.
    /// </summary>
    public required IReadOnlyList<string> FailedMessageIds { get; init; }
}
