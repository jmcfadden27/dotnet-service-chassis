namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Ready-made <see cref="ISqsConsumerPolicy"/> values.
/// </summary>
public static class SqsConsumerPolicies
{
    /// <summary>
    /// Suggested settings for a long-polling consumer: 10 messages, 20-second wait, 120-second visibility timeout, 3-second error delay and all message attributes.
    /// </summary>
    public static readonly ISqsConsumerPolicy Default = new SqsConsumerPolicy
    {
        MaxNumberOfMessages = 10,
        WaitTimeSeconds = 20,
        VisibilityTimeoutSeconds = 120,
        ErrorDelaySeconds = 3,
        MessageAttributeNames = ["All"],
    };
}
