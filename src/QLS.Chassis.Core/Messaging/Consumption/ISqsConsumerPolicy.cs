namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Settings for an SQS receive loop. This library doesn't call SQS; pass these to your own <c>ReceiveMessage</c> call.
/// </summary>
public interface ISqsConsumerPolicy
{
    /// <summary>
    /// The most messages to receive per call.
    /// </summary>
    int MaxNumberOfMessages { get; }
    /// <summary>
    /// How long a receive call waits for messages (long polling), in seconds.
    /// </summary>
    int WaitTimeSeconds { get; }
    /// <summary>
    /// How long received messages stay hidden from other consumers, in seconds.
    /// </summary>
    int VisibilityTimeoutSeconds { get; }
    /// <summary>
    /// How long to wait after a receive error before trying again, in seconds.
    /// </summary>
    int ErrorDelaySeconds { get; }
    /// <summary>
    /// The message attributes to request.
    /// </summary>
    IReadOnlyList<string> MessageAttributeNames { get; }
}
