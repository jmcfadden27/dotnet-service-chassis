namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// A settable <see cref="ISqsConsumerPolicy"/>. Numeric values default to 0 and the attribute list to empty.
/// </summary>
public sealed class SqsConsumerPolicy : ISqsConsumerPolicy
{
    /// <inheritdoc />
    public int MaxNumberOfMessages { get; init; }
    /// <inheritdoc />
    public int WaitTimeSeconds { get; init; }
    /// <inheritdoc />
    public int VisibilityTimeoutSeconds { get; init; }
    /// <inheritdoc />
    public int ErrorDelaySeconds { get; init; }
    /// <inheritdoc />
    public IReadOnlyList<string> MessageAttributeNames { get; init; } = [];
}
